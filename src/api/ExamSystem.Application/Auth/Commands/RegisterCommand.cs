namespace ExamSystem.Application.Auth.Commands;

public record RegisterCommand(
    Guid? TenantId,
    string? Username,
    string Password,
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    /// <summary>用户角色：Student 或 Teacher（默认 Student）</summary>
    string? Role = null);

public record RegisterResult(Guid UserId, string Username, string? DisplayName, string Role);
