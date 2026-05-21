using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Tenants.Commands;

public record ToggleTenantStatusCommand(Guid TenantId, bool IsActive);

public class ToggleTenantStatusCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(ToggleTenantStatusCommand command, CancellationToken cancellationToken = default)
    {
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"租户 {command.TenantId} 不存在。");

        tenant.IsActive = command.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
