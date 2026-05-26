namespace ExamSystem.Application.Auth.Commands;

public record ForgotPasswordCommand(string Identifier);

/// <param name="Message">始终返回固定提示，防止用户枚举攻击</param>
/// <param name="ResetToken">仅在非生产环境返回，生产环境应通过邮件发送</param>
public record ForgotPasswordResult(string Message, string? ResetToken);
