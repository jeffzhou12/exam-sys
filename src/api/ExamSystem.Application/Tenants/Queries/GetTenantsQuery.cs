using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Tenants.Queries;

public record GetTenantsQuery(int Page = 1, int PageSize = 10);

public record TenantDto(
    Guid Id,
    string Name,
    string SchemaName,
    string ContactEmail,
    bool IsActive,
    int AiCallQuota,
    int AiCallUsed,
    DateTime CreatedAt);

public class GetTenantsQueryHandler(IApplicationDbContext context)
{
    public async Task<PaginatedResult<TenantDto>> Handle(GetTenantsQuery query, CancellationToken cancellationToken = default)
    {
        var totalCount = await context.Tenants.LongCountAsync(cancellationToken);

        var tenants = await context.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.SchemaName, t.ContactEmail,
                t.IsActive, t.AiCallQuota, t.AiCallUsed, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return PaginatedResult<TenantDto>.Create(tenants, query.Page, query.PageSize, totalCount);
    }
}
