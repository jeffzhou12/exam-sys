using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Commands;

/// <summary>
/// 对指定考生的简答题答案触发 AI 异步评分
/// </summary>
public record GradeWithAiCommand(Guid ExamPaperId, string StudentId);

public class GradeWithAiCommandHandler(IApplicationDbContext context, IAiService aiService)
{
    public async Task<GradeWithAiResult> Handle(GradeWithAiCommand command, CancellationToken cancellationToken = default)
    {
        // 取未评分的简答题答案
        var pendingAnswers = await context.StudentAnswers
            .Include(sa => sa.Question)
            .Where(sa => sa.ExamPaperId == command.ExamPaperId
                      && sa.StudentId   == command.StudentId
                      && sa.GradingStatus == GradingStatus.Pending
                      && sa.Question.Type == QuestionType.ShortAnswer)
            .ToListAsync(cancellationToken);

        if (pendingAnswers.Count == 0)
            return new GradeWithAiResult(0, 0);

        // 取对应试卷题目分值
        var questionIds   = pendingAnswers.Select(a => a.QuestionId).ToList();
        var examQuestions = await context.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamPaperId == command.ExamPaperId && questionIds.Contains(eq.QuestionId))
            .ToDictionaryAsync(eq => eq.QuestionId, eq => eq.Score, cancellationToken);

        int gradedCount = 0, totalTokens = 0;

        foreach (var answer in pendingAnswers)
        {
            var maxScore       = examQuestions.GetValueOrDefault(answer.QuestionId, 10);
            var scoringCriteria = answer.Question.Explanation ?? "根据参考答案酌情给分";

            var result = await aiService.GradeShortAnswerAsync(
                answer.Question.CorrectAnswer,
                answer.AnswerContent,
                scoringCriteria,
                maxScore,
                cancellationToken);

            // 限制得分不超过题目满分
            answer.Score         = Math.Min(result.Score, maxScore);
            answer.AiFeedback    = result.Feedback;
            answer.GradingStatus = GradingStatus.AiGraded;

            totalTokens += result.PromptTokens + result.CompletionTokens;
            gradedCount++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new GradeWithAiResult(gradedCount, totalTokens);
    }
}

public record GradeWithAiResult(int GradedCount, int TotalTokensUsed);
