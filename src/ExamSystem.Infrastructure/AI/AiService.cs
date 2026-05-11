using ExamSystem.Application.Common.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ExamSystem.Infrastructure.AI;

/// <summary>
/// 通用 AI 服务，对接 OpenAI / Azure OpenAI 兼容接口。
/// 生产环境可替换为 Semantic Kernel 实现。
/// </summary>
public class AiService(HttpClient httpClient, AiServiceOptions options) : IAiService
{
    public async Task<string> GenerateQuestionsAsync(
        string knowledgePoint, int count, string questionType, CancellationToken cancellationToken = default)
    {
        var prompt =
            $"你是一位专业的题目出题专家。请根据以下知识点生成 {count} 道{questionType}题目。\n" +
            $"知识点：{knowledgePoint}\n\n" +
            "请以严格的 JSON 数组格式返回，每个元素包含：\n" +
            "- content: 题目内容\n- correctAnswer: 正确答案\n- explanation: 解析（可选）\n\n" +
            "只返回 JSON，不要有其他内容。";

        return await CallChatApiAsync(prompt, cancellationToken);
    }

    public async Task<AiGradingResult> GradeShortAnswerAsync(
        string referenceAnswer, string studentAnswer, string scoringCriteria, int maxScore, CancellationToken cancellationToken = default)
    {
        var prompt =
            "请根据以下评分标准，对考生的简答题作答进行评分。\n\n" +
            $"参考答案：{referenceAnswer}\n" +
            $"考生答案：{studentAnswer}\n" +
            $"评分标准：{scoringCriteria}\n" +
            $"满分：{maxScore}\n\n" +
            "请以严格的 JSON 格式返回：\n" +
            "{\"score\": <整数分数>, \"feedback\": \"<评语>\"}\n\n" +
            "只返回 JSON，不要有其他内容。";

        var raw = await CallChatApiAsync(prompt, cancellationToken);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var score = root.GetProperty("score").GetInt32();
        var feedback = root.GetProperty("feedback").GetString() ?? string.Empty;

        return new AiGradingResult(score, feedback, 0, 0);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new { model = options.EmbeddingModel, input = text };
        var response = await httpClient.PostAsJsonAsync($"{options.BaseUrl}/embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var embedding = doc!.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return embedding;
    }

    private async Task<string> CallChatApiAsync(string userMessage, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = options.ChatModel,
            messages = new[] { new { role = "user", content = userMessage } },
            temperature = 0.7
        };

        var response = await httpClient.PostAsJsonAsync($"{options.BaseUrl}/chat/completions", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        return doc!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}

public class AiServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
