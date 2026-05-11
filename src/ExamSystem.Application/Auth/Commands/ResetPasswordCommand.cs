namespace ExamSystem.Application.Auth.Commands;

public record ResetPasswordCommand(string ResetToken, string NewPassword);
