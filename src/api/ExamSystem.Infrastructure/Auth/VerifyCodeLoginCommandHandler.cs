using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

/// <summary>
/// 验证码登录/注册处理器。
/// 手机号或邮箱 + 验证码 → 若账号不存在则自动注册，返回 JWT。
/// </summary>
public class VerifyCodeLoginCommandHandler(
    IApplicationDbContext db,
    IVerificationCodeService codeService,
    IJwtTokenService jwtTokenService,
    JwtSettings jwtSettings)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<(LoginResult? Result, string? Error)> HandleAsync(
        VerifyCodeLoginCommand cmd, CancellationToken ct = default)
    {
        var target = cmd.Target.Trim();

        // 1. 校验验证码
        var valid = await codeService.ValidateAsync(target, "login", cmd.Code, ct);
        if (!valid)
            return (null, "验证码无效或已过期。");

        // 2. 判断 target 类型
        bool isEmail = target.Contains('@');

        // 3. 查找已有用户
        var user = isEmail
            ? await db.Users.FirstOrDefaultAsync(
                u => u.IsActive && u.Email != null && u.Email.ToLower() == target.ToLower(), ct)
            : await db.Users.FirstOrDefaultAsync(
                u => u.IsActive && u.PhoneNumber == target, ct);

        // 4. 若不存在则自动注册
        if (user is null)
        {
            if (!cmd.TenantId.HasValue)
                return (null, "首次登录需要选择所属机构。");

            var role = ParseRole(cmd.Role);
            var username = $"user_{Guid.NewGuid():N}"[..18];
            var placeholder = Guid.NewGuid().ToString("N"); // 验证码用户无密码，随机占位

            user = new User
            {
                TenantId    = cmd.TenantId,
                Username    = username,
                Nickname    = cmd.Nickname ?? (isEmail ? target.Split('@')[0] : target),
                Email       = isEmail ? target : null,
                PhoneNumber = isEmail ? null : target,
                PasswordHash = _hasher.HashPassword(username, placeholder),
                Role        = role,
                IsActive    = true,
            };
            db.Users.Add(user);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var roleStr = user.Role.ToString();
        var displayName = user.Nickname ?? user.Username;
        var token = jwtTokenService.GenerateToken(user.Id, displayName, roleStr, user.TenantId);

        return (new LoginResult(token, "Bearer", jwtSettings.ExpirationMinutes * 60, user.Username, displayName, roleStr), null);
    }

    private static UserRole ParseRole(string? roleStr) => roleStr?.ToLower() switch
    {
        "teacher" => UserRole.Teacher,
        _         => UserRole.Student,
    };
}
