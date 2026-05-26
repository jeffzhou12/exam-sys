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
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
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
                .AnyAsync(u => u.TenantId == command.TenantId && u.Email != null && u.Email.ToLower() == command.Email!.Trim().ToLower(), cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("该邮箱已被注册。");
        }

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            var phoneNumber = command.PhoneNumber.Trim();
            var phoneExists = await context.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.PhoneNumber == phoneNumber, cancellationToken);
            if (phoneExists)
                throw new InvalidOperationException("该手机号已被注册。");
        }

        if (!string.IsNullOrWhiteSpace(command.WeChatOpenId))
        {
            var openId = command.WeChatOpenId.Trim();
            var openIdExists = await context.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.WeChatOpenId == openId, cancellationToken);
            if (openIdExists)
                throw new InvalidOperationException("该微信账号已被绑定。");
        }

        if (!string.IsNullOrWhiteSpace(command.WeChatUnionId))
        {
            var unionId = command.WeChatUnionId.Trim();
            var unionIdExists = await context.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.WeChatUnionId == unionId, cancellationToken);
            if (unionIdExists)
                throw new InvalidOperationException("该微信账号已被绑定。");
        }

        var user = new User
        {
            TenantId = command.TenantId,
            Username = command.Username,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Nickname = command.Nickname,
            AvatarUrl = command.AvatarUrl,
            Gender = command.Gender,
            Address = command.Address,
            WeChatOpenId = command.WeChatOpenId,
            WeChatUnionId = command.WeChatUnionId,
            PasswordHash = _hasher.HashPassword(command.Username, command.Password),
            Role = command.Role,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
