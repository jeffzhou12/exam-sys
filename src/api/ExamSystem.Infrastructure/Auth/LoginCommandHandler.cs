using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

public class LoginCommandHandler(
    IJwtTokenService jwtTokenService,
    JwtSettings jwtSettings,
    IApplicationDbContext dbContext)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<LoginResult?> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var identifier = command.Identifier.Trim();
        var normalizedIdentifier = identifier.ToLower();

        // 支持用户名、邮箱、手机号、微信 openid / unionid 登录
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.IsActive &&
                (
                    u.Username == identifier ||
                    (u.Email != null && u.Email.ToLower() == normalizedIdentifier) ||
                    u.PhoneNumber == identifier ||
                    u.WeChatOpenId == identifier ||
                    u.WeChatUnionId == identifier
                ),
                cancellationToken);

        if (user is null)
            return null;

        var result = _hasher.VerifyHashedPassword(user.Username, user.PasswordHash, command.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var role = user.Role.ToString();
        var displayName = user.Nickname ?? user.Username;
        var token = jwtTokenService.GenerateToken(user.Id, displayName, role, user.TenantId);

        return new LoginResult(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: jwtSettings.ExpirationMinutes * 60,
            Username: user.Username,
            DisplayName: displayName,
            Role: role);
    }
}
