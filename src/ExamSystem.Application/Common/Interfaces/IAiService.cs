namespace ExamSystem.Application.Common.Interfaces;

public interface IAiService
{
    /// <summary>根据知识点或文档内容生成题目</summary>
    Task<string> GenerateQuestionsAsync(string knowledgePoint, int count, string questionType, CancellationToken cancellationToken = default);

    /// <summary>对简答题进行 AI 评分，返回分数和评语</summary>
    Task<AiGradingResult> GradeShortAnswerAsync(string referenceAnswer, string studentAnswer, string scoringCriteria, int maxScore, CancellationToken cancellationToken = default);

    /// <summary>生成文本的向量嵌入，用于题目去重检测</summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>对题目进行详细解析，供学生学习</summary>
    Task<string> ExplainQuestionAsync(string questionContent, string? options, string correctAnswer, string? explanation, CancellationToken cancellationToken = default);

    /// <summary>对图书段落进行 AI 分析问答</summary>
    Task<string> AnalyzeBookTextAsync(string selectedText, string question, string? bookTitle, CancellationToken cancellationToken = default);
}

public record AiGradingResult(int Score, string Feedback, int PromptTokens, int CompletionTokens);
