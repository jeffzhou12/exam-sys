using System.Security.Claims;
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
    // GET /api/exam-papers 及 /api/exam-papers/:id 为公开浏览接口（TenantId 传 null 返回全量）
    // 注意：提交答案 POST /api/exam-papers/:id/answers 不在此列，仍需租户头
    private static readonly string[] PublicPaths = ["/healthz", "/health", "/swagger", "/api/auth", "/api/tenants", "/api/users"];

    // 仅 GET 方法跳过租户校验的路径前缀（浏览考试列表/详情，不含写操作）
    // /api/media/image 用于 <img> 标签直接加载，浏览器不会附带自定义头
    // /api/books        图书列表公开浏览，不含写操作（创建/编辑/删除）
    private static readonly string[] PublicGetPaths = ["/api/exam-papers", "/api/media/image", "/api/books"];

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        // GET 试卷列表/详情不要求租户（portal 首页/考试列表公开浏览）
        // 写操作（POST /answers 等）仍需走完整租户校验
        if (context.Request.Method == HttpMethods.Get &&
            PublicGetPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        // 超级管理员无租户归属，跳过租户验证
        var userRole = context.User?.FindFirstValue(ClaimTypes.Role)
                       ?? context.User?.FindFirstValue("role");
        if (string.Equals(userRole, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
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
