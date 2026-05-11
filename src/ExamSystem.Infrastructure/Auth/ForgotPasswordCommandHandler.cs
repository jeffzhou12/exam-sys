using System.Security.Cryptography;
using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

public class ForgotPasswordCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService)
{
    private const string KeyPrefix = "pwd_reset:";
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(30);

    private static bool IsProduction =>
        string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Production",
            StringComparison.OrdinalIgnoreCase);

    public async Task<ForgotPasswordResult> HandleAsync(
        ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // 防止用户枚举：无论用户是否存在都返回相同消息
        const string genericMessage = "若该账号存在，重置链接已发送至注册邮箱，请在 30 分钟内使用。";

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                (u.Username == command.UsernameOrEmail || u.Email == command.UsernameOrEmail)
                && u.IsActive,
                cancellationToken);

        if (user is null)
            return new ForgotPasswordResult(genericMessage, null);

        // 生成加密安全随机 token（256 bit）
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe Base64

        await cacheService.SetAsync(
            KeyPrefix + token,
            user.Username,
            TokenExpiry,
            cancellationToken);

        // 仅非生产环境在响应中返回 token；生产环境应通过邮件发送
        var resetToken = IsProduction ? null : token;

        return new ForgotPasswordResult(genericMessage, resetToken);
    }
}
