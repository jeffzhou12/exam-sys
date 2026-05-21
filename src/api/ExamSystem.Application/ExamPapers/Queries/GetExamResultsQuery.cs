using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Queries;

public record GetExamResultsQuery(Guid? TenantId, Guid ExamPaperId, int Page = 1, int PageSize = 20);

public record ExamResultSummaryDto(
    string StudentId,
    int TotalScore,
    int MaxScore,
    int GradedCount,
    int PendingCount,
    DateTime? SubmittedAt);

public class GetExamResultsQueryHandler(IApplicationDbContext context)
{
    public async Task<PaginatedResult<ExamResultSummaryDto>> Handle(
        GetExamResultsQuery query, CancellationToken cancellationToken = default)
    {
        // 验证试卷属于该租户
        var paperExists = await context.ExamPapers
            .AnyAsync(e => e.Id == query.ExamPaperId &&
                (!query.TenantId.HasValue || e.TenantId == query.TenantId.Value), cancellationToken);

        if (!paperExists)
            throw new KeyNotFoundException($"试卷 {query.ExamPaperId} 不存在。");

        var answersQuery = context.StudentAnswers
            .AsNoTracking()
            .Where(a => a.ExamPaperId == query.ExamPaperId);

        var totalStudents = await answersQuery
            .Select(a => a.StudentId)
            .Distinct()
            .LongCountAsync(cancellationToken);

        var grouped = await answersQuery
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                TotalScore = g.Sum(a => a.Score ?? 0),
                GradedCount = g.Count(a => a.GradingStatus != GradingStatus.Pending),
                PendingCount = g.Count(a => a.GradingStatus == GradingStatus.Pending),
                SubmittedAt = g.Max(a => a.SubmittedAt)
            })
            .OrderByDescending(r => r.TotalScore)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = grouped.Select(r => new ExamResultSummaryDto(
            r.StudentId, r.TotalScore, 0, r.GradedCount, r.PendingCount, r.SubmittedAt)).ToList();

        return PaginatedResult<ExamResultSummaryDto>.Create(items, query.Page, query.PageSize, totalStudents);
    }
}
