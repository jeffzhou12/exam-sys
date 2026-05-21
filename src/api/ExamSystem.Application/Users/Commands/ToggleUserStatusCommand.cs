using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

public record ToggleUserStatusCommand(Guid UserId, bool IsActive);

public class ToggleUserStatusCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(ToggleUserStatusCommand command, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"用户 {command.UserId} 不存在。");

        user.IsActive = command.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
