using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Middleware;

/// <summary>
/// 多租户中间件：从请求头 X-Tenant-ID 解析租户，并验证其合法性
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    private const string TenantIdHeader = "X-Tenant-ID";

    // 跳过租户验证的路径前缀
    private static readonly string[] PublicPaths = ["/health", "/swagger", "/api/tenants"];

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var headerValue = context.Request.Headers[TenantIdHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerValue) || !Guid.TryParse(headerValue, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing or invalid '{TenantIdHeader}' header." });
            return;
        }

        var isValid = await tenantService.ValidateTenantAsync(tenantId, context.RequestAborted);
        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found or inactive." });
            return;
        }

        await next(context);
    }
}
