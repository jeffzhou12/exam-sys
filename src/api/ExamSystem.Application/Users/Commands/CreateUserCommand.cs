using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

public record CreateUserCommand(
    Guid? TenantId,
    string Username,
    string Password,
    string? Email,
    UserRole Role);

public class CreateUserCommandHandler(IApplicationDbContext context)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<Guid> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var usernameExists = await context.Users
            .AnyAsync(u => u.TenantId == command.TenantId && u.Username == command.Username, cancellationToken);

        if (usernameExists)
            throw new InvalidOperationException("该用户名已被使用。");

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailExists = await context.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.Email == command.Email, cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("该邮箱已被注册。");
        }

        var user = new User
        {
            TenantId = command.TenantId,
            Username = command.Username,
            Email = command.Email,
            PasswordHash = _hasher.HashPassword(command.Username, command.Password),
            Role = command.Role,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
