using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Queries;

public record GetExamPapersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 10,
    ExamStatus? Status = null);

public record ExamPaperDto(
    Guid Id,
    string Title,
    string? Description,
    int TotalScore,
    int DurationMinutes,
    ExamStatus Status,
    DateTime? StartTime,
    DateTime? EndTime,
    bool AntiCheatingEnabled,
    int QuestionCount,
    DateTime CreatedAt);

public class GetExamPapersQueryHandler(IApplicationDbContext context)
{
    public async Task<PaginatedResult<ExamPaperDto>> Handle(
        GetExamPapersQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.ExamPapers
            .AsNoTracking()
            .Where(e => e.TenantId == query.TenantId);

        if (query.Status.HasValue)
            q = q.Where(e => e.Status == query.Status.Value);

        var totalCount = await q.LongCountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new ExamPaperDto(
                e.Id, e.Title, e.Description, e.TotalScore, e.DurationMinutes,
                e.Status, e.StartTime, e.EndTime, e.AntiCheatingEnabled,
                e.ExamQuestions.Count, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return PaginatedResult<ExamPaperDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}
