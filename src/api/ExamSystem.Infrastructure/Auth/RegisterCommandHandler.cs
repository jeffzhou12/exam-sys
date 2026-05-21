using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

public class RegisterCommandHandler(IApplicationDbContext dbContext)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<(RegisterResult? Result, string? Error)> HandleAsync(
        RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // 验证用户名唯一性（同租户内）
        var usernameExists = await dbContext.Users
            .AnyAsync(u => u.TenantId == command.TenantId && u.Username == command.Username, cancellationToken);

        if (usernameExists)
            return (null, "该用户名已被使用。");

        // 验证邮箱唯一性（同租户内）
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailExists = await dbContext.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.Email == command.Email, cancellationToken);

            if (emailExists)
                return (null, "该邮箱已被注册。");
        }

        var user = new User
        {
            TenantId = command.TenantId,
            Username = command.Username,
            Email = command.Email,
            PasswordHash = _hasher.HashPassword(command.Username, command.Password),
            Role = UserRole.Student,
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new RegisterResult(user.Id, user.Username, user.Role.ToString()), null);
    }
}
