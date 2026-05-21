using System.Diagnostics;
using System.Security.Claims;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;

namespace ExamSystem.API.Middleware;

/// <summary>
/// 全局请求审计中间件：将每次 HTTP 请求的关键信息持久化到 audit_logs 表，
/// 同时通过 Serilog 输出结构化日志，支持运维监控与安全审计。
/// </summary>
public class RequestAuditMiddleware(
    RequestDelegate next,
    ILogger<RequestAuditMiddleware> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration)
{
    private readonly bool _persistEnabled =
        configuration.GetValue<bool>("AuditLog:Enabled", defaultValue: true);
    // 不需要审计的路径前缀（健康检查、静态资源、Swagger UI）
    private static readonly string[] SkipPaths = ["/healthz", "/health", "/swagger", "/favicon.ico"];

    // URL 路径第一段 → 实体类型名称映射
    private static readonly Dictionary<string, string> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["questions"]       = "Question",
        ["exam-papers"]     = "ExamPaper",
        ["student-answers"] = "StudentAnswer",
        ["users"]           = "User",
        ["tenants"]         = "Tenant",
        ["messages"]        = "Message",
        ["books"]           = "Book",
        ["auth"]            = "Auth",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var elapsedMs = (int)sw.ElapsedMilliseconds;

            // 结构化日志（同步，不影响响应）
            WriteLog(context, elapsedMs);

            // 数据库持久化（异步，不阻塞响应，失败时仅记录日志）
            if (_persistEnabled)
                _ = PersistAuditLogAsync(context, elapsedMs);
        }
    }

    private void WriteLog(HttpContext context, int elapsedMs)
    {
        var req        = context.Request;
        var statusCode = context.Response.StatusCode;
        var user       = context.User;
        var userId     = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "-";
        var username   = user.FindFirstValue(ClaimTypes.Name) ?? "-";
        var role       = user.FindFirstValue(ClaimTypes.Role) ?? "-";
        var tenantId   = req.Headers["X-Tenant-ID"].FirstOrDefault() ?? "-";
        var query      = req.QueryString.HasValue ? req.QueryString.Value : "";

        var level = statusCode >= 500 ? LogLevel.Error
                  : statusCode >= 400 ? LogLevel.Warning
                  : LogLevel.Information;

        logger.Log(level,
            "HTTP {Method} {Path}{Query} → {StatusCode} | {ElapsedMs}ms | User={Username}({UserId}) Role={Role} Tenant={TenantId} IP={ClientIp} TraceId={TraceId}",
            req.Method, req.Path, query, statusCode, elapsedMs,
            username, userId, role, tenantId,
            GetClientIp(context), context.TraceIdentifier);
    }

    private async Task PersistAuditLogAsync(HttpContext context, int elapsedMs)
    {
        try
        {
            var req  = context.Request;
            var user = context.User;

            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantIdStr = req.Headers["X-Tenant-ID"].FirstOrDefault();

            var (entityType, entityId) = ExtractEntity(req.Path.Value);

            var log = new AuditLog
            {
                TenantId    = Guid.TryParse(tenantIdStr, out var tid) ? tid : null,
                UserId      = Guid.TryParse(userIdStr,   out var uid) ? uid : null,
                Username    = user.FindFirstValue(ClaimTypes.Name),
                Role        = user.FindFirstValue(ClaimTypes.Role),
                Action      = req.Method,
                EntityType  = entityType,
                EntityId    = entityId,
                RequestPath = req.Path.Value ?? string.Empty,
                QueryString = req.QueryString.HasValue ? req.QueryString.Value : null,
                StatusCode  = (short)context.Response.StatusCode,
                DurationMs  = elapsedMs,
                IpAddress   = GetClientIp(context),
                UserAgent   = req.Headers.UserAgent.FirstOrDefault(),
                ErrorMessage = context.Response.StatusCode >= 400
                    ? context.Items["ErrorMessage"] as string
                    : null,
                CreatedAt = DateTime.UtcNow,
            };

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            db.AuditLogs.Add(log);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // 审计日志写入失败不应影响业务，仅记录警告
            logger.LogWarning(ex, "审计日志持久化失败，Path={Path}", context.Request.Path);
        }
    }

    /// <summary>
    /// 从 URL 路径提取资源类型和资源 ID。
    /// 例：/api/questions/uuid → ("Question", "uuid")
    /// </summary>
    private static (string? entityType, string? entityId) ExtractEntity(string? path)
    {
        if (string.IsNullOrEmpty(path)) return (null, null);

        // 路径格式：/api/{resource}/{id?}/...
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // 跳过 "api" 前缀
        var offset = segments.Length > 0 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        var resource = segments.ElementAtOrDefault(offset);
        var idSegment = segments.ElementAtOrDefault(offset + 1);

        EntityTypeMap.TryGetValue(resource ?? string.Empty, out var entityType);

        // entityId 只取看起来像 UUID 或纯数字的路径段，避免把 "generate" 之类的动作词当作 ID
        string? entityId = null;
        if (!string.IsNullOrEmpty(idSegment) &&
            (Guid.TryParse(idSegment, out _) || long.TryParse(idSegment, out _)))
        {
            entityId = idSegment;
        }

        return (entityType, entityId);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "-";
    }
}

