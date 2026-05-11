using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Commands;

public record ManualGradeCommand(Guid AnswerId, int Score, string? Feedback);

public class ManualGradeCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(ManualGradeCommand command, CancellationToken cancellationToken = default)
    {
        var answer = await context.StudentAnswers
            .FirstOrDefaultAsync(a => a.Id == command.AnswerId, cancellationToken)
            ?? throw new KeyNotFoundException($"答卷记录 {command.AnswerId} 不存在。");

        // 校验分值不超过该题目在试卷中的满分
        var maxScore = await context.ExamQuestions
            .Where(eq => eq.ExamPaperId == answer.ExamPaperId && eq.QuestionId == answer.QuestionId)
            .Select(eq => eq.Score)
            .FirstOrDefaultAsync(cancellationToken);

        if (command.Score < 0 || command.Score > maxScore)
            throw new InvalidOperationException($"分值必须在 0 到 {maxScore} 之间。");

        answer.Score = command.Score;
        answer.AiFeedback = command.Feedback;
        answer.GradingStatus = GradingStatus.ManualGraded;
        answer.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
