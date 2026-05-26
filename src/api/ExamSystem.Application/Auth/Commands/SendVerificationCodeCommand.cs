namespace ExamSystem.Application.Auth.Commands;

/// <summary>发送手机/邮箱验证码命令</summary>
public record SendVerificationCodeCommand(string Target, string Scene);

public record SendVerificationCodeResult(string Message, string? DevCode);
