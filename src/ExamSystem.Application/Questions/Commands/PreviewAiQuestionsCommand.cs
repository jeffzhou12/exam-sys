using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Application.Questions.Commands;

public record PreviewAiQuestionsCommand(
    string KnowledgePoint,
    List<AiTypeConfig> TypeConfigs);

public record AiTypeConfig(QuestionType Type, int Count, int Difficulty = 3);

public record AiQuestionPreviewDto(
    QuestionType Type,
    string Content,
    List<string>? Options,
    string CorrectAnswer,
    string? Explanation,
    string KnowledgePoint,
    int Difficulty);

public class PreviewAiQuestionsCommandHandler(IAiService aiService)
{
    public async Task<List<AiQuestionPreviewDto>> Handle(
        PreviewAiQuestionsCommand command, CancellationToken cancellationToken = default)
    {
        var tasks = command.TypeConfigs
            .Where(tc => tc.Count > 0)
            .Select(tc => GenerateForType(tc, command.KnowledgePoint, cancellationToken));

        var allResults = await Task.WhenAll(tasks);
        return allResults.SelectMany(x => x).ToList();
    }

    private async Task<List<AiQuestionPreviewDto>> GenerateForType(
        AiTypeConfig typeConfig, string knowledgePoint,
        CancellationToken cancellationToken)
    {
        var rawJson = await aiService.GenerateQuestionsAsync(
            knowledgePoint, typeConfig.Count, typeConfig.Type.ToString(), cancellationToken);

        using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
        var results = new List<AiQuestionPreviewDto>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            List<string>? options = null;
            if (item.TryGetProperty("options", out var optEl) &&
                optEl.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                options = optEl.EnumerateArray()
                    .Select(o => o.GetString() ?? string.Empty)
                    .ToList();
            }

            var diff = typeConfig.Difficulty;
            if (item.TryGetProperty("difficulty", out var diffEl) && diffEl.TryGetInt32(out var d))
                diff = Math.Clamp(d, 1, 5);

            results.Add(new AiQuestionPreviewDto(
                typeConfig.Type,
                item.GetProperty("content").GetString() ?? string.Empty,
                options,
                item.GetProperty("correctAnswer").GetString() ?? string.Empty,
                item.TryGetProperty("explanation", out var ex) ? ex.GetString() : null,
                knowledgePoint,
                diff));
        }

        return results;
    }
}
