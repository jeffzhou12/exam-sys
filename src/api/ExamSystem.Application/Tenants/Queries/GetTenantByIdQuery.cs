using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Tenants.Queries;

public record GetTenantByIdQuery(Guid TenantId);

public class GetTenantByIdQueryHandler(IApplicationDbContext context)
{
    public async Task<TenantDto?> Handle(GetTenantByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == query.TenantId)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.SchemaName, t.ContactEmail,
                t.IsActive, t.AiCallQuota, t.AiCallUsed, t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
