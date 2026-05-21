using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Application.Tenants.Commands;

public record CreateTenantCommand(
    string Name,
    string ContactEmail,
    int AiCallQuota = 1000);

public class CreateTenantCommandHandler(IApplicationDbContext context)
{
    public async Task<Guid> Handle(CreateTenantCommand command, CancellationToken cancellationToken = default)
    {
        // Schema 名称：只保留字母数字，前缀 tenant_
        var schemaName = "tenant_" + new string(command.Name
            .ToLower()
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray());

        var tenant = new Tenant
        {
            Name = command.Name,
            SchemaName = schemaName,
            ContactEmail = command.ContactEmail,
            AiCallQuota = command.AiCallQuota
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}
