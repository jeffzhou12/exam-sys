using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Favorites;

// ─── 切换收藏（添加 / 取消） ─────────────────────────────────────────────────

public record ToggleFavoriteCommand(
    Guid TenantId, string UserId, FavoriteTargetType TargetType, Guid TargetId);

/// <summary>返回操作后的状态：true=已收藏，false=已取消收藏</summary>
public class ToggleFavoriteCommandHandler(IApplicationDbContext context)
{
    public async Task<bool> Handle(ToggleFavoriteCommand cmd, CancellationToken ct = default)
    {
        var existing = await context.UserFavorites.FirstOrDefaultAsync(f =>
            f.TenantId   == cmd.TenantId   &&
            f.UserId     == cmd.UserId     &&
            f.TargetType == cmd.TargetType &&
            f.TargetId   == cmd.TargetId, ct);

        if (existing is not null)
        {
            context.UserFavorites.Remove(existing);
            await context.SaveChangesAsync(ct);
            return false;
        }

        context.UserFavorites.Add(new UserFavorite
        {
            TenantId   = cmd.TenantId,
            UserId     = cmd.UserId,
            TargetType = cmd.TargetType,
            TargetId   = cmd.TargetId,
        });
        await context.SaveChangesAsync(ct);
        return true;
    }
}

// ─── 查询是否已收藏 ──────────────────────────────────────────────────────────

public record CheckFavoriteQuery(
    Guid TenantId, string UserId, FavoriteTargetType TargetType, Guid TargetId);

public class CheckFavoriteQueryHandler(IApplicationDbContext context)
{
    public async Task<bool> Handle(CheckFavoriteQuery query, CancellationToken ct = default)
    {
        return await context.UserFavorites.AnyAsync(f =>
            f.TenantId   == query.TenantId   &&
            f.UserId     == query.UserId     &&
            f.TargetType == query.TargetType &&
            f.TargetId   == query.TargetId, ct);
    }
}

// ─── 查询收藏列表 ────────────────────────────────────────────────────────────

public record GetFavoritesQuery(
    Guid TenantId, string UserId, FavoriteTargetType TargetType, int Page = 1, int PageSize = 20);

public record FavoriteItemDto(
    Guid FavoriteId, Guid TargetId, FavoriteTargetType TargetType,
    string Title, string? Subtitle, DateTime CreatedAt);

public class GetFavoritesQueryHandler(IApplicationDbContext context)
{
    public async Task<(List<FavoriteItemDto> Items, int Total)> Handle(
        GetFavoritesQuery query, CancellationToken ct = default)
    {
        var baseQ = context.UserFavorites
            .AsNoTracking()
            .Where(f =>
                f.TenantId   == query.TenantId &&
                f.UserId     == query.UserId   &&
                f.TargetType == query.TargetType);

        var total = await baseQ.CountAsync(ct);

        var favs = await baseQ
            .OrderByDescending(f => f.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        if (favs.Count == 0)
            return ([], total);

        var ids = favs.Select(f => f.TargetId).ToList();

        List<FavoriteItemDto> items = query.TargetType switch
        {
            FavoriteTargetType.Question => await BuildQuestionItems(favs, ids, ct),
            FavoriteTargetType.ExamPaper => await BuildExamItems(favs, ids, ct),
            FavoriteTargetType.Book => await BuildBookItems(favs, ids, ct),
            _ => []
        };

        return (items, total);
    }

    private async Task<List<FavoriteItemDto>> BuildQuestionItems(
        List<UserFavorite> favs, List<Guid> ids, CancellationToken ct)
    {
        var questions = await context.Questions
            .AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .Select(q => new { q.Id, q.Content, q.KnowledgePoint })
            .ToListAsync(ct);

        return favs.Select(f =>
        {
            var q = questions.FirstOrDefault(x => x.Id == f.TargetId);
            var title = q?.Content ?? "（已删除）";
            if (title.Length > 80) title = title[..80] + "…";
            return new FavoriteItemDto(f.Id, f.TargetId, f.TargetType,
                title, q?.KnowledgePoint, f.CreatedAt);
        }).ToList();
    }

    private async Task<List<FavoriteItemDto>> BuildExamItems(
        List<UserFavorite> favs, List<Guid> ids, CancellationToken ct)
    {
        var exams = await context.ExamPapers
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Title, e.Description })
            .ToListAsync(ct);

        return favs.Select(f =>
        {
            var e = exams.FirstOrDefault(x => x.Id == f.TargetId);
            return new FavoriteItemDto(f.Id, f.TargetId, f.TargetType,
                e?.Title ?? "（已删除）", e?.Description, f.CreatedAt);
        }).ToList();
    }

    private async Task<List<FavoriteItemDto>> BuildBookItems(
        List<UserFavorite> favs, List<Guid> ids, CancellationToken ct)
    {
        var books = await context.Books
            .AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Title, b.Author })
            .ToListAsync(ct);

        return favs.Select(f =>
        {
            var b = books.FirstOrDefault(x => x.Id == f.TargetId);
            return new FavoriteItemDto(f.Id, f.TargetId, f.TargetType,
                b?.Title ?? "（已删除）", b?.Author, f.CreatedAt);
        }).ToList();
    }
}
