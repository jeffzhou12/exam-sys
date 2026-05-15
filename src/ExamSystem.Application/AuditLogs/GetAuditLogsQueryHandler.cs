using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.AuditLogs;

public record GetAuditLogsQuery(
    Guid?   TenantId   = null,
    string? Username   = null,
    string? Action     = null,
    string? EntityType = null,
    DateTime? From     = null,
    DateTime? To       = null,
    int Page           = 1,
    int PageSize       = 50);

public record AuditLogDto(
    Guid      Id,
    Guid?     TenantId,
    Guid?     UserId,
    string?   Username,
    string?   Role,
    string    Action,
    string?   EntityType,
    string?   EntityId,
    string    RequestPath,
    string?   QueryString,
    int       StatusCode,
    int       DurationMs,
    string?   IpAddress,
    string?   ErrorMessage,
    DateTime  CreatedAt);

public record GetAuditLogsResult(List<AuditLogDto> Items, int TotalCount);

public class GetAuditLogsQueryHandler(IApplicationDbContext context)
{
    public async Task<GetAuditLogsResult> Handle(
        GetAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.AuditLogs.AsNoTracking();

        if (query.TenantId.HasValue)
            q = q.Where(a => a.TenantId == query.TenantId);

        if (!string.IsNullOrWhiteSpace(query.Username))
            q = q.Where(a => a.Username != null && a.Username.Contains(query.Username));

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(a => a.Action == query.Action);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(a => a.EntityType == query.EntityType);

        if (query.From.HasValue)
            q = q.Where(a => a.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(a => a.CreatedAt <= query.To.Value);

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.TenantId,
                a.UserId,
                a.Username,
                a.Role,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.RequestPath,
                a.QueryString,
                a.StatusCode,
                a.DurationMs,
                a.IpAddress,
                a.ErrorMessage,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetAuditLogsResult(items, total);
    }
}
