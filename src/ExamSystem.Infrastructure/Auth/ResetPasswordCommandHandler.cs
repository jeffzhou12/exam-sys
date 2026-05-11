using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

public class ResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService)
{
    private const string KeyPrefix = "pwd_reset:";
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<(bool Success, string? Error)> HandleAsync(
        ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var username = await cacheService.GetAsync<string>(KeyPrefix + command.ResetToken, cancellationToken);

        if (string.IsNullOrEmpty(username))
            return (false, "重置链接无效或已过期，请重新申请。");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken);

        if (user is null)
            return (false, "用户不存在或已被禁用。");

        user.PasswordHash = _hasher.HashPassword(user.Username, command.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        // 使用后立即删除 token，防止重复使用
        await cacheService.RemoveAsync(KeyPrefix + command.ResetToken, cancellationToken);

        return (true, null);
    }
}
