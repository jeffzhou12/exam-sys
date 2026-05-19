using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Text.Json;

namespace ExamSystem.Infrastructure.AI;

/// <summary>
/// AI 服务实现。
/// 通过 IAiModelConfigService 按租户 + 场景动态解析 AI 配置，
/// 使用 OpenAI SDK（openai 官方包）以 OpenAI 兼容协议调用任意大模型。
/// </summary>
public class AiService(
    IAiModelConfigService configService,
    ITenantService tenantService,
    IApplicationDbContext dbContext,
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

        return await CallChatWithSceneAsync(AiScene.GenerateQuestions, prompt, cancellationToken);
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

        var raw = await CallChatWithSceneAsync(AiScene.GradeAnswer, prompt, cancellationToken);

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

        return await CallChatWithSceneAsync(AiScene.ExplainQuestion, prompt, cancellationToken);
    }

    public async Task<string> AnalyzeBookTextAsync(
        string selectedText, string question, string? bookTitle, string? imageBase64 = null,
        CancellationToken cancellationToken = default)
    {
        var bookPart = string.IsNullOrWhiteSpace(bookTitle) ? "" : $"（出自《{bookTitle}》）";
        var textPrompt =
            $"你是一位博学的阅读辅导助手。用户正在阅读{bookPart}，请根据他的问题进行深入解析。\n\n" +
            (string.IsNullOrWhiteSpace(selectedText) ? "" : $"【原文段落】\n{selectedText}\n\n") +
            $"【用户问题】\n{(string.IsNullOrWhiteSpace(question) ? "请分析这段内容" : question)}\n\n" +
            "请提供清晰、准确的回答，使用 Markdown 格式，结构包括：\n" +
            "1. **直接回答**：针对问题的核心解答\n" +
            "2. **深入分析**：结合原文展开讲解\n" +
            "3. **延伸思考**：相关知识或思考角度（简要）\n\n" +
            "语言生动、通俗易懂。";

        if (!string.IsNullOrWhiteSpace(imageBase64))
            return await CallVisionWithSceneAsync(AiScene.AnalyzeBook, textPrompt, imageBase64, cancellationToken);

        return await CallChatWithSceneAsync(AiScene.AnalyzeBook, textPrompt, cancellationToken);
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.TryGetCurrentTenantId();
        var config = await configService.ResolveConfigAsync(tenantId, AiScene.Embedding, cancellationToken)
            ?? throw new InvalidOperationException("未找到可用的 AI Embedding 配置，请联系管理员配置。");

        if (string.IsNullOrWhiteSpace(config.EmbeddingModel))
            throw new InvalidOperationException($"AI 配置 [{config.ProviderName}] 未指定 EmbeddingModel。");

        var (openaiClient, _) = CreateOpenAiClient(config);
        var embeddingClient = openaiClient.GetEmbeddingClient(config.EmbeddingModel);

        var result = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        var embedding = result.Value.ToFloats().ToArray();

        await TrackUsageAsync(config, "Embedding", embedding.Length, 0, true, cancellationToken);
        return embedding;
    }

    private async Task<string> CallChatWithSceneAsync(
        AiScene scene, string userMessage, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.TryGetCurrentTenantId();
        var config = await configService.ResolveConfigAsync(tenantId, scene, cancellationToken)
            ?? throw new InvalidOperationException(
                $"未找到可用的 AI 配置（场景：{scene}），请联系管理员配置。");

        logger.LogDebug("AI 调用 → 租户:{TenantId} 场景:{Scene} 提供商:{Provider} 模型:{Model}",
            tenantId, scene, config.ProviderName, config.ChatModel);

        var (openaiClient, _) = CreateOpenAiClient(config);
        var chatClient = openaiClient.GetChatClient(config.ChatModel);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("你是一个专业的教育辅助AI，请用中文回复。"),
            new UserChatMessage(userMessage)
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = config.MaxTokens,
            Temperature = (float)config.Temperature
        };

        ClientResult<ChatCompletion> response;
        try
        {
            response = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            await TrackUsageAsync(config, scene.ToString(), 0, 0, false, cancellationToken, ex.Message);
            throw;
        }

        var completion = response.Value;
        var text = completion.Content[0].Text;
        var promptTokens = completion.Usage.InputTokenCount;
        var completionTokens = completion.Usage.OutputTokenCount;

        await TrackUsageAsync(config, scene.ToString(), promptTokens, completionTokens, true, cancellationToken);
        return text;
    }

    private async Task<string> CallVisionWithSceneAsync(
        AiScene scene, string textPrompt, string imageBase64, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.TryGetCurrentTenantId();
        var config = await configService.ResolveConfigAsync(tenantId, scene, cancellationToken)
            ?? throw new InvalidOperationException(
                $"未找到可用的 AI 配置（场景：{scene}），请联系管理员配置。");

        var (openaiClient, _) = CreateOpenAiClient(config);
        var chatClient = openaiClient.GetChatClient(config.ChatModel);

        var imageData = BinaryData.FromBytes(Convert.FromBase64String(imageBase64));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("你是一个专业的教育辅助AI，请用中文回复。"),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(textPrompt),
                ChatMessageContentPart.CreateImagePart(imageData, "image/jpeg"))
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = config.MaxTokens,
            Temperature = (float)config.Temperature
        };

        ClientResult<ChatCompletion> response;
        try
        {
            response = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "视觉 API 调用失败，退回纯文本模式");
            await TrackUsageAsync(config, scene.ToString(), 0, 0, false, cancellationToken, ex.Message);
            return await CallChatWithSceneAsync(scene, textPrompt, cancellationToken);
        }

        var completion = response.Value;
        var text = completion.Content[0].Text;
        var promptTokens = completion.Usage.InputTokenCount;
        var completionTokens = completion.Usage.OutputTokenCount;

        await TrackUsageAsync(config, scene.ToString(), promptTokens, completionTokens, true, cancellationToken);
        return text;
    }

    private static (OpenAIClient client, AiModelConfig config) CreateOpenAiClient(AiModelConfig config)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl.TrimEnd('/'))
        };
        var client = new OpenAIClient(new ApiKeyCredential(config.ApiKey), clientOptions);
        return (client, config);
    }

    private async Task TrackUsageAsync(
        AiModelConfig config, string operation,
        int promptTokens, int completionTokens,
        bool isSuccess, CancellationToken ct,
        string? errorMessage = null)
    {
        var totalTokens = promptTokens + completionTokens;
        try
        {
            dbContext.AiAuditLogs.Add(new AiAuditLog
            {
                TenantId         = config.TenantId ?? Guid.Empty,
                Operation        = operation,
                ModelName        = config.ChatModel,
                PromptTokens     = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens      = totalTokens,
                IsSuccess        = isSuccess,
                ErrorMessage     = errorMessage
            });
            await dbContext.SaveChangesAsync(ct);

            if (isSuccess && totalTokens > 0)
                await configService.IncrementUsageAsync(config.Id, totalTokens, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI 审计日志写入失败");
        }
    }
}