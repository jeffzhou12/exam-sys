using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExamSystem.Application.Questions.Queries;

public record GetQuestionsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20,
    QuestionType? Type = null,
    int? Difficulty = null,
    string? KnowledgePoint = null);

public record QuestionDto(
    Guid Id,
    QuestionType Type,
    string Content,
    string? KnowledgePoint,
    int Difficulty,
    bool IsAiGenerated,
    DateTime CreatedAt);

public class GetQuestionsQueryHandler(
    IApplicationDbContext context,
    ICacheService cacheService)
{
    public async Task<PaginatedResult<QuestionDto>> Handle(GetQuestionsQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(query);
        var cached = await cacheService.GetAsync<PaginatedResult<QuestionDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var q = context.Questions
            .AsNoTracking()
            .Where(x => x.TenantId == query.TenantId && x.IsActive);

        if (query.Type.HasValue)
            q = q.Where(x => x.Type == query.Type.Value);

        if (query.Difficulty.HasValue)
            q = q.Where(x => x.Difficulty == query.Difficulty.Value);

        if (!string.IsNullOrWhiteSpace(query.KnowledgePoint))
            q = q.Where(x => x.KnowledgePoint != null && x.KnowledgePoint.Contains(query.KnowledgePoint));

        var totalCount = await q.LongCountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new QuestionDto(
                x.Id, x.Type, x.Content, x.KnowledgePoint,
                x.Difficulty, x.IsAiGenerated, x.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<QuestionDto>.Create(items, query.Page, query.PageSize, totalCount);
        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }

    private static string BuildCacheKey(GetQuestionsQuery query)
    {
        var knowledgePoint = string.IsNullOrWhiteSpace(query.KnowledgePoint)
            ? "all"
            : query.KnowledgePoint.Trim().ToLowerInvariant();

        return string.Create(CultureInfo.InvariantCulture, $"questions:{query.TenantId}:page:{query.Page}:size:{query.PageSize}:type:{query.Type?.ToString() ?? "all"}:difficulty:{query.Difficulty?.ToString(CultureInfo.InvariantCulture) ?? "all"}:knowledge:{knowledgePoint}");
    }
}
