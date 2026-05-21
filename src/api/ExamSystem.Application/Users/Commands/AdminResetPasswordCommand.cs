using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

public record AdminResetPasswordCommand(Guid UserId, string NewPassword);

public class AdminResetPasswordCommandHandler(IApplicationDbContext context)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task Handle(AdminResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"用户 {command.UserId} 不存在。");

        user.PasswordHash = _hasher.HashPassword(user.Username, command.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
