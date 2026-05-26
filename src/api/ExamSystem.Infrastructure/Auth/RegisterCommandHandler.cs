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
        var username = string.IsNullOrWhiteSpace(command.Username)
            ? $"user_{Guid.NewGuid():N}"[..18]
            : command.Username.Trim();

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber) ? null : command.PhoneNumber.Trim();
        var nickname = string.IsNullOrWhiteSpace(command.Nickname) ? null : command.Nickname.Trim();
        var avatarUrl = string.IsNullOrWhiteSpace(command.AvatarUrl) ? null : command.AvatarUrl.Trim();
        var gender = string.IsNullOrWhiteSpace(command.Gender) ? null : command.Gender.Trim();
        var address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();
        var weChatOpenId = string.IsNullOrWhiteSpace(command.WeChatOpenId) ? null : command.WeChatOpenId.Trim();
        var weChatUnionId = string.IsNullOrWhiteSpace(command.WeChatUnionId) ? null : command.WeChatUnionId.Trim();

        // 验证用户名唯一性（同租户内）
        var usernameExists = await dbContext.Users
            .AnyAsync(u => u.TenantId == command.TenantId && u.Username == username, cancellationToken);

        if (usernameExists)
            return (null, "该用户名已被使用。");

        // 验证邮箱唯一性（同租户内）
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailExists = await dbContext.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.Email != null && u.Email.ToLower() == email.ToLower(), cancellationToken);

            if (emailExists)
                return (null, "该邮箱已被注册。");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneExists = await dbContext.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.PhoneNumber == phoneNumber, cancellationToken);

            if (phoneExists)
                return (null, "该手机号已被注册。");
        }

        if (!string.IsNullOrWhiteSpace(weChatOpenId))
        {
            var openIdExists = await dbContext.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.WeChatOpenId == weChatOpenId, cancellationToken);

            if (openIdExists)
                return (null, "该微信账号已被绑定。");
        }

        if (!string.IsNullOrWhiteSpace(weChatUnionId))
        {
            var unionIdExists = await dbContext.Users
                .AnyAsync(u => u.TenantId == command.TenantId && u.WeChatUnionId == weChatUnionId, cancellationToken);

            if (unionIdExists)
                return (null, "该微信账号已被绑定。");
        }

        var role = command.Role?.ToLower() switch
        {
            "teacher" => UserRole.Teacher,
            _         => UserRole.Student,
        };

        var user = new User
        {
            TenantId = command.TenantId,
            Username = username,
            Nickname = nickname,
            AvatarUrl = avatarUrl,
            Email = email,
            PhoneNumber = phoneNumber,
            WeChatOpenId = weChatOpenId,
            WeChatUnionId = weChatUnionId,
            Gender = gender,
            Address = address,
            PasswordHash = _hasher.HashPassword(username, command.Password),
            Role = role,
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new RegisterResult(user.Id, user.Username, user.Nickname ?? user.Username, user.Role.ToString()), null);
    }
}
