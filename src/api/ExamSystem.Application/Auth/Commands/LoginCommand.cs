namespace ExamSystem.Application.Auth.Commands;

public record LoginCommand(string Identifier, string Password);

public record LoginResult(string AccessToken, string TokenType, int ExpiresIn, string Username, string? DisplayName, string Role);
