using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.AiConfigs;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AiAuditLogDto(
    Guid Id,
    Guid TenantId,
    string Operation,
    string ModelName,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    bool IsSuccess,
    string? ErrorMessage,
    Guid? RelatedEntityId,
    DateTime CreatedAt);

// ── 查询 ──────────────────────────────────────────────────────────────────────

public record GetAiAuditLogsQuery(
    Guid? TenantId,
    string? Operation,
    bool? IsSuccess,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);

public record AiAuditLogPageResult(List<AiAuditLogDto> Items, int TotalCount);

public class GetAiAuditLogsQueryHandler(IApplicationDbContext db)
{
    public async Task<AiAuditLogPageResult> Handle(GetAiAuditLogsQuery query, CancellationToken ct = default)
    {
        var q = db.AiAuditLogs.AsNoTracking();

        if (query.TenantId.HasValue)
            q = q.Where(l => l.TenantId == query.TenantId.Value);
        if (!string.IsNullOrWhiteSpace(query.Operation))
            q = q.Where(l => l.Operation.Contains(query.Operation));
        if (query.IsSuccess.HasValue)
            q = q.Where(l => l.IsSuccess == query.IsSuccess.Value);
        if (query.From.HasValue)
            q = q.Where(l => l.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(l => l.CreatedAt <= query.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AiAuditLogDto(
                l.Id, l.TenantId, l.Operation, l.ModelName,
                l.PromptTokens, l.CompletionTokens, l.TotalTokens,
                l.IsSuccess, l.ErrorMessage, l.RelatedEntityId, l.CreatedAt))
            .ToListAsync(ct);

        return new AiAuditLogPageResult(items, total);
    }
}
