using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    UserRole? Role = null,
    Guid? TenantId = null);

public class UpdateUserCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"用户 {command.UserId} 不存在。");

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber) ? null : command.PhoneNumber.Trim();
        var nickname = string.IsNullOrWhiteSpace(command.Nickname) ? null : command.Nickname.Trim();
        var avatarUrl = string.IsNullOrWhiteSpace(command.AvatarUrl) ? null : command.AvatarUrl.Trim();
        var gender = string.IsNullOrWhiteSpace(command.Gender) ? null : command.Gender.Trim();
        var address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();
        var weChatOpenId = string.IsNullOrWhiteSpace(command.WeChatOpenId) ? null : command.WeChatOpenId.Trim();
        var weChatUnionId = string.IsNullOrWhiteSpace(command.WeChatUnionId) ? null : command.WeChatUnionId.Trim();

        if (email != user.Email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                if (user.Email is not null)
                    user.Email = null;
            }
            else
            {
                var emailExists = await context.Users
                    .AnyAsync(u => u.TenantId == user.TenantId && u.Email != null && u.Email.ToLower() == email.ToLower() && u.Id != command.UserId,
                        cancellationToken);
                if (emailExists)
                    throw new InvalidOperationException("该邮箱已被其他用户使用。");
            }
        }

        if (phoneNumber != user.PhoneNumber)
        {
            var phoneExists = await context.Users
                .AnyAsync(u => u.TenantId == user.TenantId && u.PhoneNumber == phoneNumber && u.Id != command.UserId,
                    cancellationToken);
            if (phoneExists)
                throw new InvalidOperationException("该手机号已被其他用户使用。");
        }

        if (weChatOpenId != user.WeChatOpenId)
        {
            var openIdExists = await context.Users
                .AnyAsync(u => u.TenantId == user.TenantId && u.WeChatOpenId == weChatOpenId && u.Id != command.UserId,
                    cancellationToken);
            if (openIdExists)
                throw new InvalidOperationException("该微信账号已被其他用户使用。");
        }

        if (weChatUnionId != user.WeChatUnionId)
        {
            var unionIdExists = await context.Users
                .AnyAsync(u => u.TenantId == user.TenantId && u.WeChatUnionId == weChatUnionId && u.Id != command.UserId,
                    cancellationToken);
            if (unionIdExists)
                throw new InvalidOperationException("该微信账号已被其他用户使用。");
        }

        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.Nickname = nickname;
        user.AvatarUrl = avatarUrl;
        user.Gender = gender;
        user.Address = address;
        user.WeChatOpenId = weChatOpenId;
        user.WeChatUnionId = weChatUnionId;

        if (command.Role.HasValue)
            user.Role = command.Role.Value;

        if (command.TenantId.HasValue)
            user.TenantId = command.TenantId.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
