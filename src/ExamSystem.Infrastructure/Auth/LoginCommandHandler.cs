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
        // 系统级管理员（tenant_id = NULL）和普通用户均可登录
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == command.Username && u.IsActive, cancellationToken);

        if (user is null)
            return null;

        var result = _hasher.VerifyHashedPassword(user.Username, user.PasswordHash, command.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        var role = user.Role.ToString();
        var token = jwtTokenService.GenerateToken(user.Username, role, user.TenantId);

        return new LoginResult(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: jwtSettings.ExpirationMinutes * 60,
            Username: user.Username,
            Role: role);
    }
}
