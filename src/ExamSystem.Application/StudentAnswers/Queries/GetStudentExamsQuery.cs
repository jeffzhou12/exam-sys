using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Queries;

public record GetStudentExamsQuery(string StudentId, Guid? TenantId);

public record StudentExamSummaryDto(
    Guid Id,
    string Title,
    int TotalScore,
    int DurationMinutes,
    ExamStatus Status,
    DateTime? StartTime,
    DateTime? EndTime,
    int MyScore,
    DateTime? SubmittedAt,
    bool IsPending);

public class GetStudentExamsQueryHandler(IApplicationDbContext context)
{
    public async Task<List<StudentExamSummaryDto>> Handle(
        GetStudentExamsQuery query, CancellationToken ct = default)
    {
        var submittedPaperIds = await context.StudentAnswers
            .Where(sa => sa.StudentId == query.StudentId)
            .Select(sa => sa.ExamPaperId)
            .Distinct()
            .ToListAsync(ct);

        if (submittedPaperIds.Count == 0)
            return [];

        var papers = await context.ExamPapers
            .AsNoTracking()
            .Where(p => submittedPaperIds.Contains(p.Id))
            .Where(p => query.TenantId == null || p.TenantId == query.TenantId.Value)
            .OrderByDescending(p => p.EndTime ?? p.StartTime ?? p.CreatedAt)
            .ToListAsync(ct);

        var results = new List<StudentExamSummaryDto>();
        foreach (var paper in papers)
        {
            var myScore = await context.StudentAnswers
                .Where(sa => sa.StudentId == query.StudentId && sa.ExamPaperId == paper.Id)
                .SumAsync(sa => sa.Score ?? 0, ct);

            var submittedAt = await context.StudentAnswers
                .Where(sa => sa.StudentId == query.StudentId && sa.ExamPaperId == paper.Id)
                .MaxAsync(sa => (DateTime?)sa.SubmittedAt, ct);

            var hasPending = await context.StudentAnswers
                .AnyAsync(sa => sa.StudentId == query.StudentId
                             && sa.ExamPaperId == paper.Id
                             && sa.GradingStatus == GradingStatus.Pending, ct);

            results.Add(new StudentExamSummaryDto(
                paper.Id, paper.Title, paper.TotalScore, paper.DurationMinutes,
                paper.Status, paper.StartTime, paper.EndTime,
                myScore, submittedAt, hasPending));
        }

        return results;
    }
}
