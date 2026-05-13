using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using System.Text.Json;

namespace ExamSystem.Application.Questions.Commands;

public record BatchCreateQuestionsCommand(
    Guid TenantId,
    List<BatchQuestionItem> Questions);

public record BatchQuestionItem(
    QuestionType Type,
    string Content,
    List<string>? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty = 1);

public class BatchCreateQuestionsCommandHandler(
    IApplicationDbContext context,
    ICacheService cacheService)
{
    public async Task<List<Guid>> Handle(
        BatchCreateQuestionsCommand command, CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();

        foreach (var item in command.Questions)
        {
            JsonDocument? optionsDoc = null;
            if (item.Options is { Count: > 0 })
                optionsDoc = JsonDocument.Parse(JsonSerializer.Serialize(item.Options));

            var question = new Question
            {
                TenantId = command.TenantId,
                Type = item.Type,
                Content = item.Content,
                Options = optionsDoc,
                CorrectAnswer = item.CorrectAnswer,
                Explanation = item.Explanation,
                KnowledgePoint = item.KnowledgePoint,
                Difficulty = item.Difficulty,
                IsAiGenerated = true,
                IsActive = true
            };

            context.Questions.Add(question);
            ids.Add(question.Id);
        }

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);
        return ids;
    }
}
