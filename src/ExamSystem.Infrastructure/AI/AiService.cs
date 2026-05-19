using ExamSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExamSystem.Infrastructure.AI;

/// <summary>
/// AI 服务实现，主力使用 DeepSeek 官方 API，备用硅基流动 DeepSeek。
/// 当主力 Provider 请求失败时自动 Fallback 到备用 Provider。
/// </summary>
public class AiService(
    IHttpClientFactory httpClientFactory,
    AiServiceOptions options,
    ILogger<AiService> logger) : IAiService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> GenerateQuestionsAsync(
        string knowledgePoint, int count, string questionType,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            $"你是一位专业的考试出题专家。请根据以下知识点生成 {count} 道{questionType}题目。\n" +
            $"知识点：{knowledgePoint}\n\n" +
            "请以严格的 JSON 数组格式返回，每个元素包含：\n" +
            "- content: 题目内容（字符串）\n" +
            "- correctAnswer: 正确答案（字符串）\n" +
            "- explanation: 解析说明（字符串，可选）\n\n" +
            "只返回 JSON 数组，不要包含任何 Markdown 代码块或额外说明。";

        return await CallWithFallbackAsync(
            provider => CallChatApiAsync(provider, prompt, cancellationToken),
            cancellationToken);
    }

    public async Task<AiGradingResult> GradeShortAnswerAsync(
        string referenceAnswer, string studentAnswer,
        string scoringCriteria, int maxScore,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            "请根据以下评分标准，对考生的简答题作答进行客观评分。\n\n" +
            $"【参考答案】\n{referenceAnswer}\n\n" +
            $"【考生答案】\n{studentAnswer}\n\n" +
            $"【评分标准】\n{scoringCriteria}\n\n" +
            $"【满分】{maxScore} 分\n\n" +
            "请以严格的 JSON 格式返回（不包含 Markdown 代码块）：\n" +
            "{\"score\": <整数>, \"feedback\": \"<简洁评语，不超过200字>\"}";

        var raw = await CallWithFallbackAsync(
            provider => CallChatApiAsync(provider, prompt, cancellationToken),
            cancellationToken);

        // 去除可能的 markdown 代码块标记
        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            var start = raw.IndexOf('\n') + 1;
            var end = raw.LastIndexOf("```");
            raw = end > start ? raw[start..end].Trim() : raw;
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var score = root.GetProperty("score").GetInt32();
        var feedback = root.GetProperty("feedback").GetString() ?? string.Empty;

        return new AiGradingResult(score, feedback, 0, 0);
    }

    public async Task<string> ExplainQuestionAsync(
        string questionContent, string? options, string correctAnswer, string? explanation,
        CancellationToken cancellationToken = default)
    {
        var optionsPart = string.IsNullOrWhiteSpace(options) ? "" : $"\n\n【选项】\n{options}";
        var explanationPart = string.IsNullOrWhiteSpace(explanation)
            ? ""
            : $"\n\n【官方解析参考】\n{explanation}";

        var prompt =
            "你是一位耐心的学习辅导老师。请对以下题目进行详细讲解，帮助学生深入理解。\n\n" +
            $"【题目】\n{questionContent}{optionsPart}\n\n" +
            $"【正确答案】\n{correctAnswer}{explanationPart}\n\n" +
            "请从以下几个维度进行讲解（使用 Markdown 格式）：\n" +
            "1. **答案解析**：解释为什么答案是正确的\n" +
            "2. **知识点梳理**：本题涉及的核心知识点\n" +
            "3. **常见误区**：学生容易犯的错误及原因\n" +
            "4. **拓展延伸**：相关知识点的扩展（简要）\n\n" +
            "语言简洁易懂，适合学生自学。";

        return await CallWithFallbackAsync(
            provider => CallChatApiAsync(provider, prompt, cancellationToken),
            cancellationToken);
    }

    public async Task<string> AnalyzeBookTextAsync(
        string selectedText, string question, string? bookTitle, string? imageBase64 = null,
        CancellationToken cancellationToken = default)
    {
        var bookPart = string.IsNullOrWhiteSpace(bookTitle) ? "" : $"（出自《{bookTitle}》）";
        var prompt =
            $"你是一位博学的阅读辅导助手。用户正在阅读{bookPart}，请根据他的问题进行深入解析。\n\n" +
            (string.IsNullOrWhiteSpace(selectedText) ? "" : $"【原文段落】\n{selectedText}\n\n") +
            $"【用户问题】\n{(string.IsNullOrWhiteSpace(question) ? "请分析这段内容" : question)}\n\n" +
            "请提供清晰、准确的回答，使用 Markdown 格式，结构包括：\n" +
            "1. **直接回答**：针对问题的核心解答\n" +
            "2. **深入分析**：结合原文展开讲解\n" +
            "3. **延伸思考**：相关知识或思考角度（简要）\n\n" +
            "语言生动、通俗易懂。";

        if (!string.IsNullOrWhiteSpace(imageBase64))
        {
            return await CallWithFallbackAsync(
                provider => CallVisionApiAsync(provider, prompt, imageBase64, cancellationToken),
                cancellationToken);
        }

        return await CallWithFallbackAsync(
            provider => CallChatApiAsync(provider, prompt, cancellationToken),
            cancellationToken);
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        // Embedding 仅走主 Provider（DeepSeek 目前不提供 Embedding，走备用硅基流动）
        var provider = options.FallbackProvider ?? options.PrimaryProvider;

        using var client = CreateHttpClient(provider);
        var request = new { model = provider.EmbeddingModel, input = text };
        var response = await client.PostAsJsonAsync("embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);

        return doc!.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }

    // ── 内部核心方法 ──────────────────────────────────────────────────────────

    private async Task<T> CallWithFallbackAsync<T>(
        Func<AiProviderConfig, Task<T>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("AI 请求 → 主 Provider: {Provider}", options.PrimaryProvider.Name);
            return await action(options.PrimaryProvider);
        }
        catch (Exception ex) when (options.FallbackProvider is not null)
        {
            logger.LogWarning(ex,
                "主 Provider ({Primary}) 请求失败，切换到备用 Provider: {Fallback}",
                options.PrimaryProvider.Name, options.FallbackProvider.Name);
            return await action(options.FallbackProvider);
        }
    }

    private async Task<string> CallVisionApiAsync(
        AiProviderConfig provider, string textPrompt, string imageBase64, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient(provider);

        var requestBody = new
        {
            model = provider.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = "你是一个专业的教育辅助AI，请用中文回复。" },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = textPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{imageBase64}" }
                        }
                    }
                }
            },
            temperature = 0.7,
            max_tokens = 4096
        };

        var response = await client.PostAsJsonAsync("chat/completions", requestBody, JsonOpts, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // 视觉模型不可用时回退到纯文本
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"AI Vision API [{provider.Name}] 返回错误 {(int)response.StatusCode}: {errorBody}");
        }

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);

        return doc!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private async Task<string> CallChatApiAsync(
        AiProviderConfig provider, string userMessage, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient(provider);

        var requestBody = new
        {
            model = provider.ChatModel,
            messages = new[]
            {
                new { role = "system", content = "你是一个专业的教育辅助AI，请用中文回复。" },
                new { role = "user",   content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 4096
        };

        var response = await client.PostAsJsonAsync("chat/completions", requestBody, JsonOpts, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"AI API [{provider.Name}] 返回错误 {(int)response.StatusCode}: {errorBody}");
        }

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);

        return doc!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private HttpClient CreateHttpClient(AiProviderConfig provider)
    {
        var client = httpClientFactory.CreateClient("AiService");
        client.BaseAddress = new Uri(provider.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(90);
        return client;
    }
}

// ── 配置模型 ──────────────────────────────────────────────────────────────────

/// <summary>单个 AI Provider 配置</summary>
public class AiProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "deepseek-chat";
    public string EmbeddingModel { get; set; } = "BAAI/bge-m3";
}

/// <summary>AI 服务整体配置（主 + 备）</summary>
public class AiServiceOptions
{
    public AiProviderConfig PrimaryProvider { get; set; } = new();
    public AiProviderConfig? FallbackProvider { get; set; }
}

