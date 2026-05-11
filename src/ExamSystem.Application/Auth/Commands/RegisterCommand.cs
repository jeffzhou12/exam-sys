namespace ExamSystem.Application.Auth.Commands;

public record RegisterCommand(
    Guid? TenantId,
    string Username,
    string Password,
    string? Email);

public record RegisterResult(Guid UserId, string Username, string Role);
