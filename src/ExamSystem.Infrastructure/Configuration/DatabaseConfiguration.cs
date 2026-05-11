using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ExamSystem.Infrastructure.Configuration;

/// <summary>
/// 从环境变量和 AWS Secrets Manager 中构建数据库连接字符串。
/// 与原 Go 项目保持相同的环境变量约定：
///   DB_HOST, DB_PORT, DB_USER, DB_NAME, DB_SSLMODE,
///   DB_SSL_ROOT_CERT, DB_PASSWORD_SECRET_ARN (或 DB_PASSWORD)
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// 构建 Npgsql 连接字符串。
    /// 优先级：
    ///   1. 环境变量 DB_* (匹配 ECS 任务定义注入的变量)
    ///   2. 配置系统中的 DB_*（支持 appsettings / user secrets）
    ///   3. appsettings.json 中的 ConnectionStrings:DefaultConnection (本地开发)
    /// </summary>
    public static async Task<string> BuildConnectionStringAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var fallbackConnectionString = configuration.GetConnectionString("DefaultConnection");
        var host = GetSetting(configuration, "DB_HOST");

        // 如果没有 DB_HOST 环境变量，使用 appsettings 中的连接字符串（本地开发）
        if (string.IsNullOrWhiteSpace(host))
            return fallbackConnectionString ?? throw new InvalidOperationException(
                "No database configuration found. Set DB_HOST or ConnectionStrings:DefaultConnection.");

        var port     = GetSetting(configuration, "DB_PORT") ?? "5432";
        var user     = GetSetting(configuration, "DB_USER") ?? "postgres";
        var dbName   = GetSetting(configuration, "DB_NAME") ?? "exam_system";
        var sslMode  = GetSetting(configuration, "DB_SSLMODE") ?? "disable";
        var sslCert  = GetSetting(configuration, "DB_SSL_ROOT_CERT");
        var password = await ResolvePasswordAsync(configuration, cancellationToken);

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host     = host,
            Port     = int.Parse(port),
            Username = user,
            Password = password,
            Database = dbName,
        };

        // SSL 配置（生产环境使用 verify-full + RDS CA bundle）
        if (string.Equals(sslMode, "verify-full", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = Npgsql.SslMode.VerifyFull;
            if (!string.IsNullOrWhiteSpace(sslCert))
                builder.RootCertificate = sslCert;
        }
        else if (string.Equals(sslMode, "require", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = Npgsql.SslMode.Require;
        }

        // 连接池（生产默认值）
        builder.MaxPoolSize = int.Parse(GetSetting(configuration, "DB_MAX_POOL_SIZE") ?? "20");
        builder.MinPoolSize = int.Parse(GetSetting(configuration, "DB_MIN_POOL_SIZE") ?? "1");
        builder.ConnectionIdleLifetime = 300;

        return builder.ConnectionString;
    }

    /// <summary>
    /// 解析数据库密码：
    ///   1. 若 DB_PASSWORD_SECRET_ARN 已设置 → 从 AWS Secrets Manager 获取
    ///   2. 否则使用 DB_PASSWORD 环境变量
    /// </summary>
    private static async Task<string> ResolvePasswordAsync(IConfiguration configuration, CancellationToken cancellationToken)
    {
        var secretArn = GetSetting(configuration, "DB_PASSWORD_SECRET_ARN");

        if (!string.IsNullOrWhiteSpace(secretArn))
        {
            return await FetchSecretAsync(secretArn, cancellationToken);
        }

        return GetSetting(configuration, "DB_PASSWORD")
            ?? throw new InvalidOperationException(
                "Database password not configured. Set DB_PASSWORD_SECRET_ARN or DB_PASSWORD.");
    }

    private static string? GetSetting(IConfiguration configuration, string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? configuration[key];
    }

    /// <summary>
    /// 从 AWS Secrets Manager 拉取密码。
    /// RDS 自动轮转的 secret 格式为 JSON: {"password":"xxx", ...}
    /// </summary>
    private static async Task<string> FetchSecretAsync(string secretArn, CancellationToken cancellationToken)
    {
        // 从 ARN 提取 region
        var parts  = secretArn.Split(':');
        var region = parts.Length > 3 ? parts[3] : "ap-southeast-1";

        using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

        var request  = new GetSecretValueRequest { SecretId = secretArn };
        var response = await client.GetSecretValueAsync(request, cancellationToken);

        var secretString = response.SecretString
            ?? throw new InvalidOperationException($"Secret '{secretArn}' has no string value.");

        // RDS Secrets Manager 密钥格式：{"username":"...","password":"..."}
        try
        {
            using var doc = JsonDocument.Parse(secretString);
            if (doc.RootElement.TryGetProperty("password", out var pwd))
                return pwd.GetString() ?? throw new InvalidOperationException("Empty password in secret.");
        }
        catch (JsonException)
        {
            // 若 secret 直接是明文密码
        }

        return secretString;
    }
}
