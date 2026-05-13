using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

public record UpdateUserCommand(Guid UserId, string? Email, UserRole Role, Guid? TenantId = null);

public class UpdateUserCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"用户 {command.UserId} 不存在。");

        if (!string.IsNullOrWhiteSpace(command.Email) && command.Email != user.Email)
        {
            var emailExists = await context.Users
                .AnyAsync(u => u.TenantId == user.TenantId && u.Email == command.Email && u.Id != command.UserId,
                    cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("该邮箱已被其他用户使用。");
        }

        user.Email = command.Email;
        user.Role = command.Role;
        if (command.TenantId.HasValue)
            user.TenantId = command.TenantId.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
