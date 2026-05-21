using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Tenants.Commands;

public record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string ContactEmail,
    int AiCallQuota);

public class UpdateTenantCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(UpdateTenantCommand command, CancellationToken cancellationToken = default)
    {
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"租户 {command.TenantId} 不存在。");

        tenant.Name = command.Name;
        tenant.ContactEmail = command.ContactEmail;
        tenant.AiCallQuota = command.AiCallQuota;
        tenant.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
