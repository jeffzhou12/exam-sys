using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Practice;

// ─── 错题本：单条保存（upsert） ───────────────────────────────────────────────

public record SaveWrongBookItemCommand(
    Guid TenantId, string StudentId, Guid QuestionId, string AnswerGiven);

public class SaveWrongBookItemCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(SaveWrongBookItemCommand cmd, CancellationToken ct = default)
    {
        var existing = await context.WrongBookItems
            .FirstOrDefaultAsync(w =>
                w.TenantId == cmd.TenantId &&
                w.StudentId == cmd.StudentId &&
                w.QuestionId == cmd.QuestionId, ct);

        if (existing is not null)
        {
            existing.WrongCount++;
            existing.AnswerGiven = cmd.AnswerGiven;
        }
        else
        {
            context.WrongBookItems.Add(new Domain.Entities.WrongBookItem
            {
                TenantId    = cmd.TenantId,
                StudentId   = cmd.StudentId,
                QuestionId  = cmd.QuestionId,
                AnswerGiven = cmd.AnswerGiven,
                WrongCount  = 1,
            });
        }

        await context.SaveChangesAsync(ct);
    }
}

// ─── 管理端：分页查询错题本 ─────────────────────────────────────────────────

public record GetAdminWrongBookQuery(
    Guid? TenantId, string? StudentId, string? KnowledgePoint, int Page, int PageSize);

public record WrongBookItemDto(
    Guid Id, string StudentId, Guid QuestionId,
    string QuestionContent, string? KnowledgePoint, int? Difficulty,
    string AnswerGiven, int WrongCount, DateTime CreatedAt);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public class GetAdminWrongBookQueryHandler(IApplicationDbContext context)
{
    public async Task<PagedResult<WrongBookItemDto>> Handle(
        GetAdminWrongBookQuery query, CancellationToken ct = default)
    {
        var q = context.WrongBookItems
            .AsNoTracking()
            .Include(w => w.Question)
            .AsQueryable();

        if (query.TenantId.HasValue)
            q = q.Where(w => w.TenantId == query.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(query.StudentId))
            q = q.Where(w => w.StudentId == query.StudentId);

        if (!string.IsNullOrWhiteSpace(query.KnowledgePoint))
            q = q.Where(w => w.Question.KnowledgePoint != null &&
                              w.Question.KnowledgePoint.Contains(query.KnowledgePoint));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(w => w.WrongCount)
            .ThenByDescending(w => w.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(w => new WrongBookItemDto(
                w.Id, w.StudentId, w.QuestionId,
                w.Question.Content, w.Question.KnowledgePoint, w.Question.Difficulty,
                w.AnswerGiven, w.WrongCount, w.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WrongBookItemDto>(items, total, query.Page, query.PageSize);
    }
}

// ─── 管理端：分页查询练习记录 ─────────────────────────────────────────────────

public record GetAdminPracticeSessionsQuery(
    Guid? TenantId, string? StudentId, string? KnowledgePoint, int Page, int PageSize);

public record AdminPracticeSessionDto(
    Guid Id, string StudentId, int Count, int CorrectCount,
    int TotalScore, int MaxScore, string? KnowledgePoint,
    string? TypeName, int? Difficulty, DateTime CreatedAt);

public class GetAdminPracticeSessionsQueryHandler(IApplicationDbContext context)
{
    public async Task<PagedResult<AdminPracticeSessionDto>> Handle(
        GetAdminPracticeSessionsQuery query, CancellationToken ct = default)
    {
        var q = context.PracticeSessions
            .AsNoTracking()
            .AsQueryable();

        if (query.TenantId.HasValue)
            q = q.Where(s => s.TenantId == query.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(query.StudentId))
            q = q.Where(s => s.StudentId == query.StudentId);

        if (!string.IsNullOrWhiteSpace(query.KnowledgePoint))
            q = q.Where(s => s.KnowledgePoint != null &&
                              s.KnowledgePoint.Contains(query.KnowledgePoint));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AdminPracticeSessionDto(
                s.Id, s.StudentId, s.Count, s.CorrectCount,
                s.TotalScore, s.MaxScore, s.KnowledgePoint,
                s.TypeName, s.Difficulty, s.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminPracticeSessionDto>(items, total, query.Page, query.PageSize);
    }
}

// ─── AI 分析练习结果 ───────────────────────────────────────────────────────

public record AnalyzePracticeResultRequest(
    int TotalCount, int CorrectCount, int TotalScore, int MaxScore,
    string? KnowledgePoint, string? TypeName,
    List<WrongItemInfo> WrongItems);

public record WrongItemInfo(string Content, string? KnowledgePoint, int? Difficulty);

public class AnalyzePracticeResultCommandHandler(IAiService aiService)
{
    public async Task<string> Handle(AnalyzePracticeResultRequest req, CancellationToken ct = default)
    {
        var wrongItems = req.WrongItems
            .Select(w => new PracticeWrongItemInfo(w.Content, w.KnowledgePoint, w.Difficulty))
            .ToList();

        return await aiService.AnalyzePracticeResultAsync(
            req.TotalCount, req.CorrectCount, req.TotalScore, req.MaxScore,
            req.KnowledgePoint, req.TypeName, wrongItems, ct);
    }
}
