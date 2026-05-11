using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;

namespace ExamSystem.Infrastructure.MultiTenancy;

public class TenantService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext) : ITenantService
{
    private const string TenantIdHeader = "X-Tenant-ID";

    public Guid GetCurrentTenantId()
    {
        var headerValue = httpContextAccessor.HttpContext?.Request.Headers[TenantIdHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerValue) || !Guid.TryParse(headerValue, out var tenantId))
            throw new UnauthorizedAccessException("Missing or invalid X-Tenant-ID header.");

        return tenantId;
    }

    public string GetCurrentSchemaName()
    {
        var tenantId = GetCurrentTenantId();
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
