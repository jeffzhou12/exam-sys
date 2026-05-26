using System.Security.Cryptography;
using System.Text;
using ExamSystem.Application.Common.Exceptions;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Infrastructure.Auth;

public class AuthProtectionService(
    ICacheService cache,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    AuthProtectionSettings settings,
    ILogger<AuthProtectionService> logger) : IAuthProtectionService
{

    public Task AssertLoginAllowedAsync(string identifier, string? captchaToken, CancellationToken ct = default)
        => AssertAllowedAsync("login", identifier, captchaToken, settings.Login, ct);

    public Task AssertCodeLoginAllowedAsync(string target, string? captchaToken, CancellationToken ct = default)
        => AssertAllowedAsync("code-login", target, captchaToken, settings.CodeLogin, ct);

    public Task AssertRegisterAllowedAsync(string subject, string? captchaToken, CancellationToken ct = default)
        => AssertAllowedAsync("register", subject, captchaToken, settings.Register, ct);

    public Task AssertSendCodeAllowedAsync(string target, string? captchaToken, CancellationToken ct = default)
        => AssertAllowedAsync("send-code", target, captchaToken, settings.SendCode, ct);

    public AuthCaptchaClientConfig GetCaptchaClientConfig()
        => new(CaptchaEnabled());

    private async Task AssertAllowedAsync(
        string action,
        string subject,
        string? captchaToken,
        AuthProtectionSettings.RateLimitPolicy policy,
        CancellationToken ct)
    {
        var clientIp = GetClientIp();
        var subjectHash = HashSubject(subject);

        await EnforceCooldownAsync(action, clientIp, subjectHash, policy, ct);
        await EnforceWindowAsync(action, clientIp, policy, ct);
        await ValidateCaptchaAsync(action, captchaToken, clientIp, ct);
    }

    private async Task EnforceCooldownAsync(
        string action,
        string clientIp,
        string subjectHash,
        AuthProtectionSettings.RateLimitPolicy policy,
        CancellationToken ct)
    {
        if (policy.CooldownSeconds <= 0)
            return;

        var key = $"auth:protect:{action}:cooldown:{clientIp}:{subjectHash}";
        var existing = await cache.GetAsync<string>(key, ct);
        if (existing is not null)
            throw new TooManyRequestsException($"操作过于频繁，请在 {policy.CooldownSeconds} 秒后重试。");

        await cache.SetAsync(key, "1", TimeSpan.FromSeconds(policy.CooldownSeconds), ct);
    }

    private async Task EnforceWindowAsync(
        string action,
        string clientIp,
        AuthProtectionSettings.RateLimitPolicy policy,
        CancellationToken ct)
    {
        if (policy.MaxRequests <= 0 || policy.WindowMinutes <= 0)
            return;

        var now = DateTime.UtcNow;
        var windowSpan = TimeSpan.FromMinutes(policy.WindowMinutes);
        var key = $"auth:protect:{action}:window:{clientIp}";
        var existing = await cache.GetAsync<RateLimitWindowEntry>(key, ct);

        RateLimitWindowEntry next;
        if (existing is null || now - existing.WindowStartedAtUtc >= windowSpan)
        {
            next = new RateLimitWindowEntry(now, 1);
        }
        else
        {
            if (existing.Count >= policy.MaxRequests)
                throw new TooManyRequestsException($"请求过于频繁，{policy.WindowMinutes} 分钟内最多允许 {policy.MaxRequests} 次，请稍后重试。");

            next = existing with { Count = existing.Count + 1 };
        }

        var ttl = windowSpan - (now - next.WindowStartedAtUtc);
        if (ttl <= TimeSpan.Zero)
            ttl = windowSpan;

        await cache.SetAsync(key, next, ttl, ct);
    }

    private async Task ValidateCaptchaAsync(string action, string? captchaToken, string clientIp, CancellationToken ct)
    {
        if (!CaptchaEnabled())
            return;

        if (string.IsNullOrWhiteSpace(captchaToken))
            throw new InvalidOperationException("请先完成滑动验证。");

        var key = $"auth:captcha:verified:{captchaToken}";
        var stored = await cache.GetAsync<string>(key, ct);

        if (stored is null)
        {
            logger.LogWarning("滑动验证 token 无效或已过期 action={Action} ip={Ip}", action, clientIp);
            throw new InvalidOperationException("人机校验失败，请重新完成滑动验证。");
        }

        // 单次使用：立即删除
        await cache.RemoveAsync(key, ct);
    }

    private bool CaptchaEnabled()
        => settings.Captcha.Enabled && !(hostEnvironment.IsDevelopment() && settings.Captcha.BypassInDevelopment);

    private string GetClientIp()
    {
        var ctx = httpContextAccessor.HttpContext;
        var forwardedFor = ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return ctx?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string HashSubject(string subject)
    {
        var normalized = subject.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private sealed record RateLimitWindowEntry(DateTime WindowStartedAtUtc, int Count);
}