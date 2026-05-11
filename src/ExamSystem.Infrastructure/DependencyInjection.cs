using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.AI;
using ExamSystem.Infrastructure.Caching;
using ExamSystem.Infrastructure.Configuration;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseNpgsql(
                connStr,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsql.CommandTimeout(30);
                    npgsql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // ── Redis 缓存 ─────────────────────────────────────────────────────────
        var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
            ?? configuration["Redis:Connection"]
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConn));

        services.AddSingleton<ICacheService, CacheService>();

        // ── 多租户 ─────────────────────────────────────────────────────────────
        services.AddScoped<ITenantService, TenantService>();

        // ── AI 服务 ────────────────────────────────────────────────────────────
        var aiOptions = configuration.GetSection("AI").Get<AiServiceOptions>()
            ?? new AiServiceOptions();

        // 环境变量覆盖 appsettings（生产环境通过 ECS 注入）
        var apiKeyFromEnv = Environment.GetEnvironmentVariable("AI__APIKEY")
            ?? Environment.GetEnvironmentVariable("AI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKeyFromEnv))
            aiOptions.ApiKey = apiKeyFromEnv;

        var baseUrlFromEnv = Environment.GetEnvironmentVariable("AI__BASEURL")
            ?? Environment.GetEnvironmentVariable("AI_BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrlFromEnv))
            aiOptions.BaseUrl = baseUrlFromEnv;

        services.AddSingleton(aiOptions);
        services.AddHttpClient<AiService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<AiServiceOptions>();
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<IAiService, AiService>();

        return services;
    }
}
