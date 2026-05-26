namespace ExamSystem.Application.Common.Interfaces;

public interface IAuthProtectionService
{
    Task AssertLoginAllowedAsync(string identifier, string? captchaToken, CancellationToken ct = default);
    Task AssertCodeLoginAllowedAsync(string target, string? captchaToken, CancellationToken ct = default);
    Task AssertRegisterAllowedAsync(string subject, string? captchaToken, CancellationToken ct = default);
    Task AssertSendCodeAllowedAsync(string target, string? captchaToken, CancellationToken ct = default);
    AuthCaptchaClientConfig GetCaptchaClientConfig();
}

/// <summary>返回给前端的验证码配置：是否启用（前端据此决定是否显示滑动验证码）。</summary>
public record AuthCaptchaClientConfig(bool Enabled);