using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Application.Questions.Commands;

public record GenerateQuestionsWithAiCommand(
    Guid TenantId,
    string KnowledgePoint,
    QuestionType QuestionType,
    int Count = 5);

public class GenerateQuestionsWithAiCommandHandler(
    IApplicationDbContext context,
    IAiService aiService,
    ICacheService cacheService)
{
    public async Task<int> Handle(GenerateQuestionsWithAiCommand command, CancellationToken cancellationToken = default)
    {
        var rawJson = await aiService.GenerateQuestionsAsync(
            command.KnowledgePoint,
            command.Count,
            command.QuestionType.ToString(),
            cancellationToken);

        // 解析 AI 返回的 JSON 并入库
        using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        int created = 0;
        foreach (var item in root.EnumerateArray())
        {
            var question = new Question
            {
                TenantId = command.TenantId,
                Type = command.QuestionType,
                Content = item.GetProperty("content").GetString() ?? string.Empty,
                CorrectAnswer = item.GetProperty("correctAnswer").GetString() ?? string.Empty,
                Explanation = item.TryGetProperty("explanation", out var ex) ? ex.GetString() : null,
                KnowledgePoint = command.KnowledgePoint,
                IsAiGenerated = true
            };

            context.Questions.Add(question);
            created++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);

        return created;
    }
}
