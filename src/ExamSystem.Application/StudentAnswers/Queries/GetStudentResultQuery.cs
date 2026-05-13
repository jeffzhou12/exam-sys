using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Queries;

public record GetStudentResultQuery(Guid ExamPaperId, string StudentId);

public record StudentResultDto(
    Guid ExamPaperId,
    string StudentId,
    int TotalScore,
    int MaxScore,
    string GradingStatus,
    List<AnswerResultItem> Answers);

public record AnswerResultItem(
    Guid AnswerId,
    Guid QuestionId,
    string QuestionContent,
    string AnswerContent,
    int? Score,
    int MaxScore,
    string GradingStatus,
    string? AiFeedback);

public class GetStudentResultQueryHandler(IApplicationDbContext context)
{
    public async Task<StudentResultDto> Handle(
        GetStudentResultQuery query, CancellationToken cancellationToken = default)
    {
        var paper = await context.ExamPapers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.ExamPaperId, cancellationToken)
            ?? throw new KeyNotFoundException($"试卷 {query.ExamPaperId} 不存在。");

        var answers = await context.StudentAnswers
            .AsNoTracking()
            .Include(sa => sa.Question)
            .Where(sa => sa.ExamPaperId == query.ExamPaperId && sa.StudentId == query.StudentId)
            .ToListAsync(cancellationToken);

        var examQuestions = await context.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamPaperId == query.ExamPaperId)
            .ToDictionaryAsync(eq => eq.QuestionId, eq => eq.Score, cancellationToken);

        var totalScore  = answers.Sum(a => a.Score ?? 0);
        var overallStatus = DetermineOverallStatus(answers.Select(a => a.GradingStatus));

        var answerResults = answers.Select(a => new AnswerResultItem(
            a.Id,
            a.QuestionId,
            a.Question.Content,
            a.AnswerContent,
            a.Score,
            examQuestions.GetValueOrDefault(a.QuestionId, 0),
            a.GradingStatus.ToString(),
            a.AiFeedback))
            .ToList();

        return new StudentResultDto(
            query.ExamPaperId, query.StudentId,
            totalScore, paper.TotalScore,
            overallStatus, answerResults);
    }

    private static string DetermineOverallStatus(IEnumerable<GradingStatus> statuses)
    {
        var list = statuses.ToList();
        if (list.All(s => s == GradingStatus.ManualGraded)) return "ManualGraded";
        if (list.Any(s => s == GradingStatus.Pending))      return "Pending";
        if (list.All(s => s == GradingStatus.AutoGraded))   return "AutoGraded";
        return "PartiallyGraded";
    }
}
