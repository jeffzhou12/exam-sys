using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using System.Text.Json;

namespace ExamSystem.Application.Questions.Commands;

public record CreateQuestionCommand(
    Guid TenantId,
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty = 1);

public class CreateQuestionCommandHandler(
    IApplicationDbContext context,
    ICacheService cacheService)
{
    public async Task<Guid> Handle(CreateQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = new Question
        {
            TenantId = command.TenantId,
            Type = command.Type,
            Content = command.Content,
            Options = command.Options,
            CorrectAnswer = command.CorrectAnswer,
            Explanation = command.Explanation,
            KnowledgePoint = command.KnowledgePoint,
            Difficulty = command.Difficulty
        };

        context.Questions.Add(question);
        await context.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);

        return question.Id;
    }
}
