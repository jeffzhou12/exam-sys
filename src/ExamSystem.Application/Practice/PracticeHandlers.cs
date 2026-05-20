using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.StudentAnswers.Commands;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamSystem.Application.Practice;

// ─────────────────────────────────────────────────────────────────────────────
// 获取练习题目（不含正确答案，防止作弊）
// ─────────────────────────────────────────────────────────────────────────────

public record GetPracticeQuestionsQuery(
    Guid TenantId,
    int Count = 10,
    QuestionType? Type = null,
    int? Difficulty = null,
    string? KnowledgePoint = null);

public record PracticeQuestionDto(
    Guid Id,
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string? KnowledgePoint,
    int Difficulty);

public class GetPracticeQuestionsQueryHandler(IApplicationDbContext context)
{
    public async Task<List<PracticeQuestionDto>> Handle(
        GetPracticeQuestionsQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.Questions.AsNoTracking()
            .Where(x => x.IsActive && x.TenantId == query.TenantId);

        if (query.Type.HasValue)
            q = q.Where(x => x.Type == query.Type.Value);

        if (query.Difficulty.HasValue)
            q = q.Where(x => x.Difficulty == query.Difficulty.Value);

        if (!string.IsNullOrWhiteSpace(query.KnowledgePoint))
            q = q.Where(x => x.KnowledgePoint != null && x.KnowledgePoint.Contains(query.KnowledgePoint));

        // 随机抽取
        var items = await q
            .OrderBy(_ => EF.Functions.Random())
            .Take(Math.Min(query.Count, 100))
            .Select(x => new PracticeQuestionDto(
                x.Id, x.Type, x.Content, x.Options, x.KnowledgePoint, x.Difficulty))
            .ToListAsync(cancellationToken);

        return items;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 提交练习答案并自动批改
// ─────────────────────────────────────────────────────────────────────────────

public record PracticeAnswerItem(Guid QuestionId, string Answer);

public record SubmitPracticeCommand(Guid TenantId, List<PracticeAnswerItem> Answers);

public record PracticeResultItemDto(
    Guid QuestionId,
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string StudentAnswer,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty,
    bool IsCorrect,
    int Score,
    int MaxScore);

public record SubmitPracticeResult(
    List<PracticeResultItemDto> Items,
    int TotalScore,
    int MaxScore,
    int CorrectCount);

public class SubmitPracticeCommandHandler(IApplicationDbContext context)
{


    public async Task<SubmitPracticeResult> Handle(
        SubmitPracticeCommand command, CancellationToken cancellationToken = default)
    {
        var ids = command.Answers.Select(a => a.QuestionId).ToList();

        var questions = await context.Questions.AsNoTracking()
            .Where(q => ids.Contains(q.Id) && q.TenantId == command.TenantId)
            .ToListAsync(cancellationToken);

        var results = new List<PracticeResultItemDto>();
        int totalScore = 0;
        int maxScore = 0;
        int correctCount = 0;

        foreach (var ans in command.Answers)
        {
            var q = questions.FirstOrDefault(x => x.Id == ans.QuestionId);
            if (q is null) continue;

            const int scorePerQuestion = 1;
            bool isCorrect = false;

            if (q.Type == QuestionType.ShortAnswer)
            {
                // 简答题：不自动评分，留给学生自评
                isCorrect = false;
            }
            else if (q.Type == QuestionType.MultipleChoice)
            {
                // 多选题：规范化后排序比较，兼容 "ABC" 和 "A,B,C" 两种格式
                isCorrect = AnswerNormalizer.MultiEquals(ans.Answer, q.CorrectAnswer);
            }
            else if (q.Type == QuestionType.TrueFalse)
            {
                // 判断题：兼容 True/False（前端）与 正确/错误（数据库）两种格式
                isCorrect = AnswerNormalizer.BoolEquals(ans.Answer, q.CorrectAnswer);
            }
            else
            {
                // 单选题：忽略大小写直接比较
                isCorrect = string.Equals(
                    ans.Answer.Trim(), q.CorrectAnswer.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }

            var score = isCorrect ? scorePerQuestion : 0;
            totalScore += score;
            maxScore += scorePerQuestion;
            if (isCorrect) correctCount++;

            results.Add(new PracticeResultItemDto(
                q.Id, q.Type, q.Content, q.Options,
                ForDisplay(q.Type, ans.Answer),
                ForDisplay(q.Type, q.CorrectAnswer),
                q.Explanation,
                q.KnowledgePoint, q.Difficulty,
                isCorrect, score, scorePerQuestion));
        }

        return new SubmitPracticeResult(results, totalScore, maxScore, correctCount);
    }

    /// <summary>将答案规范化为统一的展示格式（前端呈现用）。</summary>
    private static string ForDisplay(QuestionType type, string answer) => type switch
    {
        QuestionType.MultipleChoice =>
            string.Join(",", AnswerNormalizer.NormalizeMulti(answer)),
        QuestionType.TrueFalse =>
            AnswerNormalizer.NormalizeBool(answer) switch
            {
                true  => "正确",
                false => "错误",
                _     => answer,
            },
        _ => answer,
    };
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取相似题目（按知识点 + 难度匹配）
// ─────────────────────────────────────────────────────────────────────────────

public record GetSimilarQuestionsQuery(
    Guid TenantId,
    Guid ExcludeQuestionId,
    string? KnowledgePoint,
    int Difficulty,
    int Count = 5);

public class GetSimilarQuestionsQueryHandler(IApplicationDbContext context)
{
    public async Task<List<PracticeQuestionDto>> Handle(
        GetSimilarQuestionsQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.Questions.AsNoTracking()
            .Where(x => x.IsActive
                && x.TenantId == query.TenantId
                && x.Id != query.ExcludeQuestionId);

        if (!string.IsNullOrWhiteSpace(query.KnowledgePoint))
            q = q.Where(x => x.KnowledgePoint != null && x.KnowledgePoint.Contains(query.KnowledgePoint));

        // 难度相近：±1
        q = q.Where(x => x.Difficulty >= query.Difficulty - 1 && x.Difficulty <= query.Difficulty + 1);

        return await q
            .OrderBy(_ => EF.Functions.Random())
            .Take(query.Count)
            .Select(x => new PracticeQuestionDto(
                x.Id, x.Type, x.Content, x.Options, x.KnowledgePoint, x.Difficulty))
            .ToListAsync(cancellationToken);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// AI 解题分析
// ─────────────────────────────────────────────────────────────────────────────

public record ExplainQuestionCommand(Guid TenantId, Guid QuestionId);

public class ExplainQuestionCommandHandler(IApplicationDbContext context, IAiService aiService)
{
    public async Task<string> Handle(
        ExplainQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var q = await context.Questions.AsNoTracking()
            .Where(x => x.Id == command.QuestionId && x.TenantId == command.TenantId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("题目不存在。");

        string? optionsText = null;
        if (q.Options is not null)
        {
            try
            {
                optionsText = q.Options.RootElement.GetRawText();
            }
            catch { /* ignore */ }
        }

        return await aiService.ExplainQuestionAsync(
            q.Content, optionsText, q.CorrectAnswer, q.Explanation, cancellationToken);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取题目参考答案（供学生自查）
// ─────────────────────────────────────────────────────────────────────────────

public record GetQuestionAnswerQuery(Guid TenantId, Guid QuestionId);

public record QuestionAnswerDto(string CorrectAnswer, string? Explanation);

public class GetQuestionAnswerQueryHandler(IApplicationDbContext context)
{
    public async Task<QuestionAnswerDto?> Handle(
        GetQuestionAnswerQuery query, CancellationToken cancellationToken = default)
    {
        var q = await context.Questions.AsNoTracking()
            .Where(x => x.Id == query.QuestionId && x.TenantId == query.TenantId)
            .Select(x => new { x.Type, x.CorrectAnswer, x.Explanation })
            .FirstOrDefaultAsync(cancellationToken);

        if (q is null) return null;

        // 多选题：将 "ABC" 规范化为 "A,B,C" 便于前端展示
        var displayAnswer = q.Type == QuestionType.MultipleChoice
            ? string.Join(",", AnswerNormalizer.NormalizeMulti(q.CorrectAnswer))
            : q.CorrectAnswer;

        return new QuestionAnswerDto(displayAnswer, q.Explanation);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 保存练习会话记录到服务端
// ─────────────────────────────────────────────────────────────────────────────

public record SavePracticeSessionCommand(
    Guid TenantId,
    string StudentId,
    int Count,
    int CorrectCount,
    int TotalScore,
    int MaxScore,
    string? TypeName,
    string? KnowledgePoint,
    int? QuestionType,
    int? Difficulty,
    int SetupCount);

public class SavePracticeSessionCommandHandler(IApplicationDbContext context)
{
    public async Task<Guid> Handle(SavePracticeSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = new Domain.Entities.PracticeSession
        {
            TenantId      = command.TenantId,
            StudentId     = command.StudentId,
            Count         = command.Count,
            CorrectCount  = command.CorrectCount,
            TotalScore    = command.TotalScore,
            MaxScore      = command.MaxScore,
            TypeName      = command.TypeName,
            KnowledgePoint = command.KnowledgePoint,
            QuestionType  = command.QuestionType,
            Difficulty    = command.Difficulty,
            SetupCount    = command.SetupCount,
        };
        context.PracticeSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return session.Id;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 查询练习历史记录列表
// ─────────────────────────────────────────────────────────────────────────────

public record GetPracticeHistoryQuery(Guid TenantId, string StudentId, int PageSize = 20);

public record PracticeSessionDto(
    Guid Id,
    int Count,
    int CorrectCount,
    int TotalScore,
    int MaxScore,
    double CorrectRate,
    string? TypeName,
    string? KnowledgePoint,
    int? QuestionType,
    int? Difficulty,
    int SetupCount,
    DateTime CreatedAt);

public class GetPracticeHistoryQueryHandler(IApplicationDbContext context)
{
    public async Task<List<PracticeSessionDto>> Handle(
        GetPracticeHistoryQuery query, CancellationToken cancellationToken = default)
    {
        return await context.PracticeSessions.AsNoTracking()
            .Where(p => p.TenantId == query.TenantId && p.StudentId == query.StudentId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(query.PageSize)
            .Select(p => new PracticeSessionDto(
                p.Id,
                p.Count,
                p.CorrectCount,
                p.TotalScore,
                p.MaxScore,
                p.MaxScore > 0 ? (double)p.TotalScore / p.MaxScore : 0,
                p.TypeName,
                p.KnowledgePoint,
                p.QuestionType,
                p.Difficulty,
                p.SetupCount,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
