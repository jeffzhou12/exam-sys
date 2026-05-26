namespace ExamSystem.Application.Common.Interfaces;

/// <summary>
/// 手机/邮箱验证码服务接口（用于快速登录/注册）。
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// 为指定目标（手机号或邮箱）生成并存储验证码，有效期 5 分钟。
    /// 生产环境应通过短信/邮件发送；开发环境直接返回码以便调试。
    /// </summary>
    /// <param name="target">手机号或邮箱</param>
    /// <param name="scene">场景标识，如 "login" "register"</param>
    /// <returns>开发环境返回明文验证码；生产环境返回 null</returns>
    Task<string?> SendCodeAsync(string target, string scene, CancellationToken ct = default);

    /// <summary>
    /// 校验验证码。验证通过后立即失效（防重放）。
    /// </summary>
    Task<bool> ValidateAsync(string target, string scene, string code, CancellationToken ct = default);
}
