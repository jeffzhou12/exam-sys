using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Infrastructure.Auth;

/// <summary>
/// 基于 ICacheService（Redis）的验证码服务。
/// 生产环境需替换 SendCodeAsync 内的发送逻辑（集成短信 SDK 或邮件服务）。
/// </summary>
public class VerificationCodeService(
    ICacheService cache,
    IHostEnvironment env,
    ITenantService tenantService,
    ISmsTemplateService smsTemplateService,
    ISmsSender smsSender,
    TwilioSmsSettings smsSettings,
    ILogger<VerificationCodeService> logger) : IVerificationCodeService
{
    private static readonly TimeSpan CodeExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockoutExpiry = TimeSpan.FromMinutes(1);
    private const string DefaultTemplate = "您的验证码是 {code}，5 分钟内有效。";

    private static string CacheKey(string target, string scene) =>
        $"vcode:{scene}:{target.ToLower()}";

    private static string LockKey(string target, string scene) =>
        $"vcode:lock:{scene}:{target.ToLower()}";

    public async Task<string?> SendCodeAsync(string target, string scene, CancellationToken ct = default)
    {
        // 防刷：1 分钟内不允许重复发送
        var lockKey = LockKey(target, scene);
        var existing = await cache.GetAsync<string>(lockKey, ct);
        if (existing is not null)
            throw new InvalidOperationException("发送过于频繁，请 1 分钟后再试。");

        var code = GenerateCode();

        await cache.SetAsync(CacheKey(target, scene), code, CodeExpiry, ct);
        await cache.SetAsync(lockKey, "1", LockoutExpiry, ct);

        if (env.IsDevelopment())
        {
            logger.LogInformation("[DEV] 验证码 target={Target} scene={Scene} code={Code}", target, scene, code);
            return code;   // 开发环境直接返回，便于调试
        }

        var tenantId = tenantService.TryGetCurrentTenantId();
        var template = await smsTemplateService.ResolveAsync(tenantId, scene, ct);
        var body = RenderTemplate(template?.TemplateBody ?? DefaultTemplate, target, scene, code, smsSettings.AppName);

        await smsSender.SendAsync(target, body, ct);

        logger.LogInformation("验证码已发送 target={Target} scene={Scene} tenantId={TenantId}", target, scene, tenantId);
        return null;
    }

    public async Task<bool> ValidateAsync(string target, string scene, string code, CancellationToken ct = default)
    {
        var key = CacheKey(target, scene);
        var stored = await cache.GetAsync<string>(key, ct);
        if (stored is null || !string.Equals(stored, code.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        // 一次性：验证通过即删除
        await cache.RemoveAsync(key, ct);
        return true;
    }

    private static string GenerateCode()
    {
        var rng = Random.Shared;
        return rng.Next(100000, 999999).ToString();
    }

    private static string RenderTemplate(string template, string target, string scene, string code, string appName)
        => template
            .Replace("{code}", code, StringComparison.OrdinalIgnoreCase)
            .Replace("{scene}", scene, StringComparison.OrdinalIgnoreCase)
            .Replace("{target}", target, StringComparison.OrdinalIgnoreCase)
            .Replace("{appName}", appName, StringComparison.OrdinalIgnoreCase);
}
