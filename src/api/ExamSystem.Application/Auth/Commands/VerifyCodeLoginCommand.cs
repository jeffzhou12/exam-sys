namespace ExamSystem.Application.Auth.Commands;

/// <summary>
/// 使用手机/邮箱 + 验证码登录（不存在则自动注册）。
/// </summary>
public record VerifyCodeLoginCommand(
    string Target,
    string Code,
    Guid? TenantId,
    string? Role,
    string? Nickname);
