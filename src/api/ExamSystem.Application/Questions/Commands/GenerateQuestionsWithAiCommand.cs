using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExamSystem.Application.Questions.Commands;

public record GenerateQuestionsWithAiCommand(
    Guid TenantId,
    string? KnowledgePoint,
    List<AiTypeConfig> TypeConfigs);

public class GenerateQuestionsWithAiCommandHandler(
    IApplicationDbContext context,
    IAiService aiService,
    ICacheService cacheService)
{
    public async Task<int> Handle(GenerateQuestionsWithAiCommand command, CancellationToken cancellationToken = default)
    {
        int created = 0;
        var configs = command.TypeConfigs
            .Where(t => t.Count > 0)
            .ToList();

        foreach (var typeConfig in configs)
        {
            var knowledgePoint = typeConfig.KnowledgePoint ?? command.KnowledgePoint;
            if (string.IsNullOrWhiteSpace(knowledgePoint))
                throw new InvalidOperationException("请为每个生成配置提供知识点。");

            var rawJson = await aiService.GenerateQuestionsAsync(
                knowledgePoint,
                typeConfig.Count,
                typeConfig.Type.ToString(),
                typeConfig.Difficulty,
                cancellationToken);

            // 兼容模型偶发返回代码块包裹/前后说明文本
            var json = ExtractJsonArrayText(rawJson);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var item in root.EnumerateArray())
            {
                var content = item.TryGetProperty("content", out var contentEl)
                    ? contentEl.GetString() ?? string.Empty
                    : string.Empty;

                var optionsDoc = BuildOptionsDocument(typeConfig.Type, item, ref content);
                var correctAnswerRaw = item.TryGetProperty("correctAnswer", out var answerEl)
                    ? answerEl.GetString() ?? string.Empty
                    : string.Empty;

                var question = new Question
                {
                    TenantId = command.TenantId,
                    Type = typeConfig.Type,
                    Content = content.Trim(),
                    Options = optionsDoc,
                    CorrectAnswer = NormalizeCorrectAnswer(typeConfig.Type, correctAnswerRaw),
                    Explanation = item.TryGetProperty("explanation", out var ex) ? ex.GetString() : null,
                    KnowledgePoint = knowledgePoint,
                    Difficulty = typeConfig.Difficulty,
                    IsAiGenerated = true
                };

                if (item.TryGetProperty("difficulty", out var difficultyEl) && difficultyEl.TryGetInt32(out var d))
                    question.Difficulty = Math.Clamp(d, 1, 5);

                context.Questions.Add(question);
                created++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);

        return created;
    }

    private static bool IsChoiceType(QuestionType type) =>
        type is QuestionType.SingleChoice or QuestionType.MultipleChoice;

    private static JsonDocument? BuildOptionsDocument(QuestionType type, JsonElement item, ref string content)
    {
        if (!IsChoiceType(type))
            return null;

        var options = ParseOptionsFromJson(item);
        if (options.Count == 0)
            options = ExtractOptionsFromContent(ref content);

        if (options.Count == 0)
            return null;

        var normalized = options
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value.Trim());

        if (normalized.Count == 0)
            return null;

        return JsonDocument.Parse(JsonSerializer.Serialize(normalized));
    }

    private static Dictionary<string, string> ParseOptionsFromJson(JsonElement item)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!item.TryGetProperty("options", out var optionsEl))
            return options;

        if (optionsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in optionsEl.EnumerateObject())
            {
                var key = NormalizeOptionKey(property.Name);
                if (key is null)
                    continue;

                var value = property.Value.GetString() ?? property.Value.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    options[key] = value.Trim();
            }
            return options;
        }

        if (optionsEl.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var element in optionsEl.EnumerateArray())
            {
                var text = element.GetString() ?? element.ToString();
                text = text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (TrySplitLabeledOption(text, out var key, out var content))
                    options[key] = content;
                else
                {
                    var autoKey = ((char)('A' + i)).ToString();
                    options[autoKey] = text;
                }

                i++;
            }
        }

        return options;
    }

    private static Dictionary<string, string> ExtractOptionsFromContent(ref string content)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var remainingLines = new List<string>();

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (TrySplitLabeledOption(trimmed, out var key, out var value))
                options[key] = value;
            else
                remainingLines.Add(line);
        }

        if (options.Count >= 2)
            content = string.Join("\n", remainingLines).Trim();

        return options.Count >= 2
            ? options
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TrySplitLabeledOption(string text, out string key, out string value)
    {
        var match = Regex.Match(text, "^\\s*([A-Ha-h])(?:[\\.、:：\\)）])\\s*(.+?)\\s*$");
        if (match.Success)
        {
            key = match.Groups[1].Value.ToUpperInvariant();
            value = match.Groups[2].Value.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        key = string.Empty;
        value = string.Empty;
        return false;
    }

    private static string? NormalizeOptionKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return null;

        var ch = char.ToUpperInvariant(rawKey.Trim()[0]);
        return ch is >= 'A' and <= 'H' ? ch.ToString() : null;
    }

    private static string NormalizeCorrectAnswer(QuestionType type, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        if (type == QuestionType.TrueFalse)
        {
            var normalized = raw.Trim().ToLowerInvariant();
            if (normalized is "true" or "t" or "1" or "对" or "正确")
                return "True";
            if (normalized is "false" or "f" or "0" or "错" or "错误")
                return "False";
            return raw.Trim();
        }

        if (!IsChoiceType(type))
            return raw.Trim();

        var letters = Regex.Matches(raw.ToUpperInvariant(), "[A-H]")
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (letters.Count == 0)
            return raw.Trim();

        return type == QuestionType.SingleChoice ? letters[0] : string.Join(string.Empty, letters);
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
