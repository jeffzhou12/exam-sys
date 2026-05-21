using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamSystem.Application.Questions.Commands;

public record UpdateQuestionCommand(
    Guid TenantId,
    Guid QuestionId,
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty);

public class UpdateQuestionCommandHandler(
    IApplicationDbContext context,
    ICacheService cacheService)
{
    public async Task Handle(UpdateQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = await context.Questions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId && q.TenantId == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"题目 {command.QuestionId} 不存在。");

        question.Type = command.Type;
        question.Content = command.Content;
        question.Options = command.Options;
        question.CorrectAnswer = command.CorrectAnswer;
        question.Explanation = command.Explanation;
        question.KnowledgePoint = command.KnowledgePoint;
        question.Difficulty = command.Difficulty;
        question.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);
    }
}
