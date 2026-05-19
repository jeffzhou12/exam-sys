using Amazon;
using Amazon.S3;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Infrastructure.AI;
using ExamSystem.Infrastructure.Auth;
using ExamSystem.Infrastructure.Caching;
using ExamSystem.Infrastructure.Configuration;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Infrastructure.MultiTenancy;
using ExamSystem.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ExamSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL / EF Core ───────────────────────────────────────────────
        // 异步构建连接字符串（支持 AWS Secrets Manager）
        var connStr = DatabaseConfiguration
            .BuildConnectionStringAsync(configuration)
            .GetAwaiter().GetResult();

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(
                    connStr,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        npgsql.CommandTimeout(30);
                        npgsql.EnableRetryOnFailure(3);
                    })
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // ── Redis 缓存 ─────────────────────────────────────────────────────────
        var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
            ?? configuration["Redis:Connection"]
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConn);
            options.AbortOnConnectFail = false;   // 连接失败不抛异常，返回断开状态的 Multiplexer
            options.ConnectTimeout = 3000;
            options.SyncTimeout = 3000;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, ResilientCacheService>();

        // ── 多租户 ─────────────────────────────────────────────────────────────
        services.AddScoped<ITenantService, TenantService>();

        // ── JWT ────────────────────────────────────────────────────────────────
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        var secretKeyFromEnv = Environment.GetEnvironmentVariable("JWT__SECRETKEY")
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrWhiteSpace(secretKeyFromEnv))
            jwtSettings = jwtSettings with { SecretKey = secretKeyFromEnv };

        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        // ── AI 服务 ────────────────────────────────────────────────────────────
        var aiOptions = new AiServiceOptions
        {
            PrimaryProvider = new AiProviderConfig
            {
                Name         = "DeepSeek",
                BaseUrl      = configuration["AI:Primary:BaseUrl"]  ?? "https://api.deepseek.com/v1",
                ApiKey       = configuration["AI:Primary:ApiKey"]   ?? string.Empty,
                ChatModel    = configuration["AI:Primary:ChatModel"] ?? "deepseek-chat",
                EmbeddingModel = "BAAI/bge-m3"   // DeepSeek 不提供 Embedding，此处备用
            },
            FallbackProvider = new AiProviderConfig
            {
                Name         = "SiliconFlow-DeepSeek",
                BaseUrl      = configuration["AI:Fallback:BaseUrl"]  ?? "https://api.siliconflow.cn/v1",
                ApiKey       = configuration["AI:Fallback:ApiKey"]   ?? string.Empty,
                ChatModel    = configuration["AI:Fallback:ChatModel"] ?? "deepseek-ai/DeepSeek-V3",
                EmbeddingModel = configuration["AI:Fallback:EmbeddingModel"] ?? "BAAI/bge-m3"
            }
        };

        // 环境变量优先覆盖（ECS 生产环境注入）
        var primaryKey  = Environment.GetEnvironmentVariable("AI__PRIMARY__APIKEY");
        var fallbackKey = Environment.GetEnvironmentVariable("AI__FALLBACK__APIKEY");
        if (!string.IsNullOrWhiteSpace(primaryKey))  aiOptions.PrimaryProvider.ApiKey  = primaryKey;
        if (!string.IsNullOrWhiteSpace(fallbackKey)) aiOptions.FallbackProvider.ApiKey = fallbackKey;

        // 备用 Provider ApiKey 为空时禁用 Fallback（避免无意义的二次调用）
        if (string.IsNullOrWhiteSpace(aiOptions.FallbackProvider.ApiKey))
            aiOptions.FallbackProvider = null;

        services.AddSingleton(aiOptions);
        services.AddHttpClient("AiService")
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.Delay = TimeSpan.FromSeconds(1);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
            });
        services.AddScoped<IAiService, AiService>();

        // ── 文件存储（Provider 开关 + 模块级 Bucket 路由）────────────────────────
        var storageSettings = configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();

        // 环境变量优先覆盖 Provider（ECS 生产环境注入）
        var providerFromEnv = Environment.GetEnvironmentVariable("STORAGE__PROVIDER");
        if (!string.IsNullOrWhiteSpace(providerFromEnv))
            storageSettings.Provider = providerFromEnv;

        // 向后兼容：旧版 AWS__S3__BUCKETNAME 环境变量 → 写入 Default Bucket
        var legacyBucket = Environment.GetEnvironmentVariable("AWS__S3__BUCKETNAME");
        if (!string.IsNullOrWhiteSpace(legacyBucket) && !storageSettings.S3.Buckets.ContainsKey("Default"))
            storageSettings.S3.Buckets["Default"] = legacyBucket;

        services.AddSingleton(storageSettings);

        if (storageSettings.Provider.Equals("s3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(_ =>
                new AmazonS3Client(RegionEndpoint.GetBySystemName(storageSettings.S3.Region)));

            services.AddSingleton<IFileStorageFactory>(sp =>
            {
                var s3 = sp.GetRequiredService<IAmazonS3>();
                var expiry = storageSettings.S3.PresignedUrlExpirationMinutes;
                var storages = new Dictionary<string, IFileStorageService>(StringComparer.OrdinalIgnoreCase);

                // 每个已配置的 Bucket 建立独立实例
                foreach (var (module, bucket) in storageSettings.S3.Buckets)
                {
                    if (!string.IsNullOrWhiteSpace(bucket))
                        storages[module] = new S3FileStorageService(s3, bucket, expiry);
                }

                if (storages.Count == 0)
                    throw new InvalidOperationException(
                        "Storage:Provider 为 s3，但 Storage:S3:Buckets 中没有任何有效的 Bucket 配置。");

                // 若未显式配置 Default，用第一个已配置模块的实例作为兜底
                if (!storages.ContainsKey("Default"))
                    storages["Default"] = storages.Values.First();

                return new FileStorageFactory(storages);
            });
        }
        else
        {
            // 本地模式：所有模块共享同一本地存储（以 subfolder 区分）
            services.AddSingleton<IFileStorageFactory>(sp =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                var local = new LocalFileStorageService(env);
                var storages = new Dictionary<string, IFileStorageService>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Default"] = local
                };
                return new FileStorageFactory(storages);
            });
        }

        // 向后兼容：直接注入 IFileStorageService 的代码自动使用 Default 存储
        services.AddScoped<IFileStorageService>(sp =>
            sp.GetRequiredService<IFileStorageFactory>().GetStorage("Default"));

        return services;
    }
}
