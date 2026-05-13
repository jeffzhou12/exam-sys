using System.Security.Claims;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;

namespace ExamSystem.Infrastructure.MultiTenancy;

public class TenantService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext) : ITenantService
{
    private const string TenantIdHeader = "X-Tenant-ID";

    public Guid? GetCurrentTenantId()
    {
        var ctx = httpContextAccessor.HttpContext;
        var headerValue = ctx?.Request.Headers[TenantIdHeader].FirstOrDefault();

        // 如果请求头中有有效的租户 ID，直接使用（所有角色均适用）
        if (!string.IsNullOrWhiteSpace(headerValue) && Guid.TryParse(headerValue, out var tenantId))
            return tenantId;

        // 超级管理员没有选择租户时，返回 null（表示不限制租户）
        var role = ctx?.User?.FindFirstValue(ClaimTypes.Role)
                   ?? ctx?.User?.FindFirstValue("role");
        if (string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
            return null;

        throw new UnauthorizedAccessException("Missing or invalid X-Tenant-ID header.");
    }

    public Guid? TryGetCurrentTenantId()
    {
        var ctx = httpContextAccessor.HttpContext;
        var headerValue = ctx?.Request.Headers[TenantIdHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerValue) && Guid.TryParse(headerValue, out var tenantId))
            return tenantId;
        // 无 Header 或解析失败时返回 null，不抛异常
        return null;
    }

    public string GetCurrentSchemaName()
    {
        var tenantId = GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("SuperAdmin must select a tenant before this operation.");
        // 同步查询 schema 名称（通常已缓存）
        var tenant = dbContext.Tenants.AsNoTracking()
            .FirstOrDefault(t => t.Id == tenantId && t.IsActive)
            ?? throw new UnauthorizedAccessException($"Tenant '{tenantId}' not found or inactive.");

        return tenant.SchemaName;
    }

    public async Task<bool> ValidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
    }
}
