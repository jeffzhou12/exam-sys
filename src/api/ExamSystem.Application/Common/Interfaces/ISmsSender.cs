namespace ExamSystem.Application.Common.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string to, string body, CancellationToken ct = default);
}