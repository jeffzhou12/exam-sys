namespace ExamSystem.Application.Auth.Commands;

public record LoginCommand(string Username, string Password);

public record LoginResult(string AccessToken, string TokenType, int ExpiresIn, string Username, string Role);
