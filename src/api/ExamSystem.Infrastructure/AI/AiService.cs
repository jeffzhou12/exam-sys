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
        string knowledgePoint, int count, string questionType, int? difficulty = null,
        CancellationToken cancellationToken = default)
    {
        var typeRequirement = questionType switch
        {
            nameof(QuestionType.SingleChoice) =>
                "- options: 选项对象（对象，键必须是 A/B/C/D，值为选项内容）\n" +
                "- correctAnswer: 正确答案（只能是 A/B/C/D 中一个字母）\n" +
                "- 注意：content 只包含题干，不要把选项写进 content。\n",
            nameof(QuestionType.MultipleChoice) =>
                "- options: 选项对象（对象，键必须是 A/B/C/D，值为选项内容）\n" +
                "- correctAnswer: 正确答案（多个字母连续字符串，如 AC 或 BCD，按字母升序）\n" +
                "- 注意：content 只包含题干，不要把选项写进 content。\n",
            nameof(QuestionType.TrueFalse) =>
                "- correctAnswer: 正确答案（只能是 True 或 False）\n" +
                "- options 不需要返回。\n",
            _ => "- correctAnswer: 正确答案（字符串）\n- options 不需要返回。\n"
        };

        var prompt =
            $"你是一位专业的考试出题专家。请根据以下知识点生成 {count} 道与{questionType}相关的题目。\n" +
            $"知识点：{knowledgePoint}\n\n" +
            $"目标难度：{(difficulty.HasValue ? difficulty.Value.ToString() : "由你根据知识点自行判断，范围 1~5")}\n" +
            "要求：如果返回 difficulty 字段，必须是 1~5 的整数；若未返回，系统将按目标难度兜底。\n\n" +
            "请以严格的 JSON 数组格式返回，每个元素包含：\n" +
            "- content: 题目内容（字符串）\n" +
            typeRequirement +
            "- difficulty: 难度系数（整数 1~5，建议返回）\n" +
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
        bool hasText  = !string.IsNullOrWhiteSpace(selectedText);
        bool hasImage = !string.IsNullOrWhiteSpace(imageBase64);

        string textPrompt;
        if (!hasText && hasImage)
        {
            // 纯图片区域（扫描页、图表、公式等）—— 要求 AI 先做 OCR 再分析
            textPrompt =
                $"你是一位博学的阅读辅导助手。用户正在阅读{bookPart}，框选了一段图片区域（可能是扫描文字、图表、公式或插图）。\n\n" +
                "**请先完整地识别图片中的所有文字内容（OCR）**，然后对识别结果进行深入分析。\n\n" +
                $"【用户问题】\n{(string.IsNullOrWhiteSpace(question) ? "请识别并分析图中的内容" : question)}\n\n" +
                "请使用 Markdown 格式，结构包括：\n" +
                "1. **图片内容识别**：逐字列出图片中识别到的文字（若为纯图形则描述图形内容）\n" +
                "2. **分析解读**：对识别内容进行深入分析讲解\n" +
                "3. **延伸思考**：相关知识或思考角度（简要）\n\n" +
                "语言生动、通俗易懂。";
        }
        else
        {
            textPrompt =
                $"你是一位博学的阅读辅导助手。用户正在阅读{bookPart}，请根据他的问题进行深入解析。\n\n" +
                (hasText ? $"【原文段落】\n{selectedText}\n\n" : "") +
                $"【用户问题】\n{(string.IsNullOrWhiteSpace(question) ? "请分析这段内容" : question)}\n\n" +
                "请提供清晰、准确的回答，使用 Markdown 格式，结构包括：\n" +
                "1. **直接回答**：针对问题的核心解答\n" +
                "2. **深入分析**：结合原文展开讲解\n" +
                "3. **延伸思考**：相关知识或思考角度（简要）\n\n" +
                "语言生动、通俗易懂。";
        }

        if (hasImage)
            return await CallVisionWithSceneAsync(AiScene.AnalyzeBook, textPrompt, imageBase64!, cancellationToken);

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

    public async Task<string> AnalyzeExamResultAsync(
        string examTitle, int totalScore, int maxScore, double percentage,
        List<string> wrongQuestionSummaries,
        CancellationToken cancellationToken = default)
    {
        var pctStr = percentage.ToString("F0");
        var wrongPart = wrongQuestionSummaries.Count > 0
            ? $"\n\n【答错的题目（部分）】\n{string.Join('\n', wrongQuestionSummaries)}"
            : "\n\n（全部答对，表现优异！）";

        var prompt =
            $"你是一位专业的学习分析师和教育顾问。请对以下考试成绩进行深度分析。\n\n" +
            $"【考试名称】{examTitle}\n" +
            $"【得分】{totalScore} / {maxScore}（{pctStr}%）{wrongPart}\n\n" +
            "请从以下维度进行深入分析（使用 Markdown 格式，条理清晰）：\n" +
            "1. **整体评价**：对本次考试成绩的综合评价（1-2 句）\n" +
            "2. **薄弱知识点分析**：根据答错题目，分析存在薄弱的知识点或概念\n" +
            "3. **深层原因诊断**：推测可能的学习误区或理解偏差\n" +
            "4. **针对性提升建议**：给出 3-5 条具体可执行的学习建议\n" +
            "5. **推荐练习方向**：建议重点练习的题型或知识领域\n\n" +
            "语言亲切、鼓励为主，具体可执行，避免空话套话。";

        return await CallChatWithSceneAsync(AiScene.AnalyzePerformance, prompt, cancellationToken);
    }

    public async Task<string> AnalyzePracticeResultAsync(
        int totalCount, int correctCount, int totalScore, int maxScore,
        string? knowledgePoint, string? typeName,
        List<PracticeWrongItemInfo> wrongItems,
        CancellationToken cancellationToken = default)
    {
        var correctRate = totalCount > 0 ? (double)correctCount / totalCount * 100 : 0;
        var kpPart = string.IsNullOrWhiteSpace(knowledgePoint) ? "综合练习" : $"知识点：{knowledgePoint}";
        var typePart = string.IsNullOrWhiteSpace(typeName) ? "" : $"，题型：{typeName}";
        var wrongPart = wrongItems.Count > 0
            ? "\n\n【答错的题目（部分）】\n" + string.Join('\n', wrongItems.Take(10).Select(w =>
                $"- {w.Content[..Math.Min(80, w.Content.Length)]}（知识点：{w.KnowledgePoint ?? "未标注"}，难度：{w.Difficulty?.ToString() ?? "未知"}）"))
            : "\n\n（全部答对！）";

        var prompt =
            $"你是一位专业的学习分析师。请对以下在线练习结果进行深度分析。\n\n" +
            $"【练习范围】{kpPart}{typePart}\n" +
            $"【总题数】{totalCount} 题，【答对】{correctCount} 题（正确率 {correctRate:F0}%）\n" +
            $"【得分】{totalScore} / {maxScore}{wrongPart}\n\n" +
            "请从以下维度进行分析（使用 Markdown 格式）：\n" +
            "1. **练习表现评价**：简要评价本次练习\n" +
            "2. **知识掌握诊断**：分析哪些知识点掌握不扎实\n" +
            "3. **错误规律分析**：找出错题中的规律（题型、难度、知识点集中度）\n" +
            "4. **改进建议**：3-5 条针对性练习建议\n" +
            "5. **下一步学习计划**：推荐接下来的练习重点\n\n" +
            "语言简洁、鼓励，帮助学生建立信心并制定有效学习计划。";

        return await CallChatWithSceneAsync(AiScene.AnalyzePerformance, prompt, cancellationToken);
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