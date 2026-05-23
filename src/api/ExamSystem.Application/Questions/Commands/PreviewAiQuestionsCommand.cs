using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExamSystem.Application.Questions.Commands;

public record PreviewAiQuestionsCommand(
    string KnowledgePoint,
    List<AiTypeConfig> TypeConfigs);

public record AiTypeConfig(QuestionType Type, int Count, int Difficulty = 3, string? KnowledgePoint = null);

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
        var resolvedKnowledgePoint = typeConfig.KnowledgePoint ?? knowledgePoint;
        var rawJson = await aiService.GenerateQuestionsAsync(
            resolvedKnowledgePoint, typeConfig.Count, typeConfig.Type.ToString(), typeConfig.Difficulty, cancellationToken);

        using var doc = JsonDocument.Parse(ExtractJsonArrayText(rawJson));
        var results = new List<AiQuestionPreviewDto>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var content = item.TryGetProperty("content", out var contentEl)
                ? contentEl.GetString() ?? string.Empty
                : string.Empty;

            var options = ExtractOptions(item, ref content);

            var diff = typeConfig.Difficulty;
            if (item.TryGetProperty("difficulty", out var diffEl) && diffEl.TryGetInt32(out var d))
                diff = Math.Clamp(d, 1, 5);

            results.Add(new AiQuestionPreviewDto(
                typeConfig.Type,
                content.Trim(),
                options,
                item.GetProperty("correctAnswer").GetString() ?? string.Empty,
                item.TryGetProperty("explanation", out var ex) ? ex.GetString() : null,
                resolvedKnowledgePoint,
                diff));
        }

        return results;
    }

    private static List<string>? ExtractOptions(JsonElement item, ref string content)
    {
        var values = new List<string>();

        if (item.TryGetProperty("options", out var optEl))
        {
            if (optEl.ValueKind == JsonValueKind.Array)
            {
                values = optEl.EnumerateArray()
                    .Select(o => (o.GetString() ?? o.ToString()).Trim())
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .Select(RemoveOptionPrefix)
                    .ToList();
            }
            else if (optEl.ValueKind == JsonValueKind.Object)
            {
                values = optEl.EnumerateObject()
                    .OrderBy(p => p.Name)
                    .Select(p => (p.Value.GetString() ?? p.Value.ToString()).Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();
            }
        }

        if (values.Count > 0)
            return values;

        var extracted = new List<(string Label, string Text)>();
        var remained = new List<string>();
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            var match = Regex.Match(trimmed, "^\\s*([A-Ha-h])(?:[\\.、:：\\)）])\\s*(.+?)\\s*$");
            if (match.Success)
                extracted.Add((match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.Trim()));
            else
                remained.Add(line);
        }

        if (extracted.Count < 2)
            return null;

        content = string.Join("\n", remained).Trim();
        return extracted
            .OrderBy(x => x.Label)
            .Select(x => x.Text)
            .ToList();
    }

    private static string RemoveOptionPrefix(string value)
    {
        var match = Regex.Match(value, "^\\s*[A-Ha-h](?:[\\.、:：\\)）])\\s*(.+?)\\s*$");
        return match.Success ? match.Groups[1].Value.Trim() : value;
    }

    private static string ExtractJsonArrayText(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = text.IndexOf('\n');
            var codeStart = firstLineEnd >= 0 ? firstLineEnd + 1 : 0;
            var codeEnd = text.LastIndexOf("```", StringComparison.Ordinal);
            if (codeEnd > codeStart)
                text = text[codeStart..codeEnd].Trim();
        }

        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }
}
