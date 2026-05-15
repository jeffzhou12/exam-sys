using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamSystem.Application.Books;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record BookDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Author,
    string? Publisher,
    string? Description,
    string? CoverImageUrl,
    string? Category,
    List<string> Tags,
    int? PublishYear,
    string? Isbn,
    int PageCount,
    long FileSizeBytes,
    bool IsActive,
    bool HasPdf,
    string UploadedByName,
    DateTime CreatedAt
);

public record BookAnnotationDto(
    Guid Id,
    Guid BookId,
    Guid UserId,
    string UserName,
    int PageNumber,
    string? SelectedText,
    string? Note,
    int AnnotationType,
    string? AiQuestion,
    string? AiAnswer,
    string? PositionJson,
    string HighlightColor,
    DateTime CreatedAt
);

// ─── Queries ──────────────────────────────────────────────────────────────────

public record GetBooksQuery(
    Guid? TenantId,
    string? Category = null,
    string? Tag = null,
    string? Keyword = null,
    bool? IsActive = true,
    int Page = 1,
    int PageSize = 20
);

public record GetBookByIdQuery(Guid Id, Guid TenantId);

public record GetBookAnnotationsQuery(Guid BookId, Guid UserId);

// ─── Commands ─────────────────────────────────────────────────────────────────

public record CreateBookCommand(
    Guid TenantId,
    Guid OperatorId,
    string OperatorName,
    string Title,
    string? Author,
    string? Publisher,
    string? Description,
    string? CoverImageUrl,
    string? Category,
    List<string>? Tags,
    int? PublishYear,
    string? Isbn
);

public record UpdateBookCommand(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Author,
    string? Publisher,
    string? Description,
    string? CoverImageUrl,
    string? Category,
    List<string>? Tags,
    int? PublishYear,
    string? Isbn,
    bool IsActive
);

public record UploadBookPdfCommand(Guid Id, Guid TenantId, Stream PdfStream, string FileName, long FileSize);

public record DeleteBookCommand(Guid Id, Guid TenantId);

public record CreateAnnotationCommand(
    Guid BookId,
    Guid UserId,
    string UserName,
    int PageNumber,
    string? SelectedText,
    string? Note,
    int AnnotationType,
    string? AiQuestion,
    string? PositionJson,
    string HighlightColor = "#FFEB3B"
);

public record UpdateAnnotationCommand(Guid Id, Guid UserId, string? Note, string? AiQuestion, string HighlightColor);

public record DeleteAnnotationCommand(Guid Id, Guid UserId);

public record AiAnalyzeTextCommand(Guid BookId, Guid TenantId, string SelectedText, string Question);

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetBooksQueryHandler(IApplicationDbContext db)
{
    public async Task<PaginatedResult<BookDto>> Handle(GetBooksQuery q, CancellationToken ct = default)
    {
        var query = q.TenantId.HasValue
            ? db.Books.Where(b => b.TenantId == q.TenantId.Value)
            : db.Books.AsQueryable();

        if (q.IsActive.HasValue)
            query = query.Where(b => b.IsActive == q.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(b => b.Category == q.Category);

        if (!string.IsNullOrWhiteSpace(q.Keyword))
            query = query.Where(b =>
                b.Title.Contains(q.Keyword) ||
                (b.Author != null && b.Author.Contains(q.Keyword)) ||
                (b.Description != null && b.Description.Contains(q.Keyword)));

        if (!string.IsNullOrWhiteSpace(q.Tag))
            query = query.Where(b => b.Tags != null && b.Tags.Contains(q.Tag));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<BookDto>.Create(
            items.Select(ToDto).ToList(),
            q.Page, q.PageSize, total);
    }

    private static BookDto ToDto(Book b) => new(
        b.Id, b.TenantId, b.Title, b.Author, b.Publisher, b.Description,
        b.CoverImageUrl, b.Category,
        ParseTags(b.Tags),
        b.PublishYear, b.Isbn, b.PageCount, b.FileSizeBytes,
        b.IsActive, !string.IsNullOrEmpty(b.PdfFilePath),
        b.UploadedByName, b.CreatedAt);

    private static List<string> ParseTags(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
}

public class GetBookByIdQueryHandler(IApplicationDbContext db)
{
    public async Task<BookDto?> Handle(GetBookByIdQuery q, CancellationToken ct = default)
    {
        var b = await db.Books.FirstOrDefaultAsync(x => x.Id == q.Id && x.TenantId == q.TenantId, ct);
        if (b is null) return null;
        return new BookDto(
            b.Id, b.TenantId, b.Title, b.Author, b.Publisher, b.Description,
            b.CoverImageUrl, b.Category,
            ParseTags(b.Tags),
            b.PublishYear, b.Isbn, b.PageCount, b.FileSizeBytes,
            b.IsActive, !string.IsNullOrEmpty(b.PdfFilePath),
            b.UploadedByName, b.CreatedAt);
    }

    private static List<string> ParseTags(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
}

public class CreateBookCommandHandler(IApplicationDbContext db)
{
    public async Task<Guid> Handle(CreateBookCommand cmd, CancellationToken ct = default)
    {
        var book = new Book
        {
            TenantId = cmd.TenantId,
            Title = cmd.Title,
            Author = cmd.Author,
            Publisher = cmd.Publisher,
            Description = cmd.Description,
            CoverImageUrl = cmd.CoverImageUrl,
            Category = cmd.Category,
            Tags = cmd.Tags?.Count > 0 ? JsonSerializer.Serialize(cmd.Tags) : null,
            PublishYear = cmd.PublishYear,
            Isbn = cmd.Isbn,
            IsActive = true,
            UploadedById = cmd.OperatorId,
            UploadedByName = cmd.OperatorName,
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book.Id;
    }
}

public class UpdateBookCommandHandler(IApplicationDbContext db)
{
    public async Task Handle(UpdateBookCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct)
            ?? throw new KeyNotFoundException("图书不存在");
        book.Title = cmd.Title;
        book.Author = cmd.Author;
        book.Publisher = cmd.Publisher;
        book.Description = cmd.Description;
        book.CoverImageUrl = cmd.CoverImageUrl;
        book.Category = cmd.Category;
        book.Tags = cmd.Tags?.Count > 0 ? JsonSerializer.Serialize(cmd.Tags) : null;
        book.PublishYear = cmd.PublishYear;
        book.Isbn = cmd.Isbn;
        book.IsActive = cmd.IsActive;
        await db.SaveChangesAsync(ct);
    }
}

public class UploadBookPdfCommandHandler(IApplicationDbContext db, IFileStorageService fileStorage)
{
    public async Task Handle(UploadBookPdfCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct)
            ?? throw new KeyNotFoundException("图书不存在");

        // 删除旧文件
        if (!string.IsNullOrEmpty(book.PdfFilePath))
            await fileStorage.DeleteAsync(book.PdfFilePath);

        var relativePath = await fileStorage.SaveAsync(cmd.PdfStream, cmd.FileName, $"books/{cmd.TenantId}", ct);

        book.PdfFilePath = relativePath;
        book.FileSizeBytes = cmd.FileSize;
        await db.SaveChangesAsync(ct);
    }
}

public class DeleteBookCommandHandler(IApplicationDbContext db, IFileStorageService fileStorage)
{
    public async Task Handle(DeleteBookCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.Id && b.TenantId == cmd.TenantId, ct)
            ?? throw new KeyNotFoundException("图书不存在");

        if (!string.IsNullOrEmpty(book.PdfFilePath))
            await fileStorage.DeleteAsync(book.PdfFilePath);

        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
    }
}

// ─── Annotation handlers ──────────────────────────────────────────────────────

public class GetBookAnnotationsQueryHandler(IApplicationDbContext db)
{
    public async Task<List<BookAnnotationDto>> Handle(GetBookAnnotationsQuery q, CancellationToken ct = default)
    {
        return await db.BookAnnotations
            .Where(a => a.BookId == q.BookId && a.UserId == q.UserId)
            .OrderBy(a => a.PageNumber).ThenBy(a => a.CreatedAt)
            .Select(a => new BookAnnotationDto(
                a.Id, a.BookId, a.UserId, a.UserName, a.PageNumber,
                a.SelectedText, a.Note, a.AnnotationType,
                a.AiQuestion, a.AiAnswer, a.PositionJson, a.HighlightColor, a.CreatedAt))
            .ToListAsync(ct);
    }
}

public class CreateAnnotationCommandHandler(IApplicationDbContext db, IAiService aiService)
{
    public async Task<BookAnnotationDto> Handle(CreateAnnotationCommand cmd, CancellationToken ct = default)
    {
        var ann = new BookAnnotation
        {
            BookId = cmd.BookId,
            UserId = cmd.UserId,
            UserName = cmd.UserName,
            PageNumber = cmd.PageNumber,
            SelectedText = cmd.SelectedText,
            Note = cmd.Note,
            AnnotationType = cmd.AnnotationType,
            AiQuestion = cmd.AiQuestion,
            PositionJson = cmd.PositionJson,
            HighlightColor = cmd.HighlightColor,
        };

        // 如果是 AI 问答类型，立即调用 AI
        if (cmd.AnnotationType == 3 && !string.IsNullOrWhiteSpace(cmd.SelectedText) && !string.IsNullOrWhiteSpace(cmd.AiQuestion))
        {
            var book = await db.Books.FindAsync([cmd.BookId], ct);
            ann.AiAnswer = await aiService.AnalyzeBookTextAsync(
                cmd.SelectedText, cmd.AiQuestion, book?.Title, ct);
        }

        db.BookAnnotations.Add(ann);
        await db.SaveChangesAsync(ct);

        return new BookAnnotationDto(
            ann.Id, ann.BookId, ann.UserId, ann.UserName, ann.PageNumber,
            ann.SelectedText, ann.Note, ann.AnnotationType,
            ann.AiQuestion, ann.AiAnswer, ann.PositionJson, ann.HighlightColor, ann.CreatedAt);
    }
}

public class UpdateAnnotationCommandHandler(IApplicationDbContext db)
{
    public async Task Handle(UpdateAnnotationCommand cmd, CancellationToken ct = default)
    {
        var ann = await db.BookAnnotations.FirstOrDefaultAsync(a => a.Id == cmd.Id && a.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("标注不存在");
        ann.Note = cmd.Note;
        ann.HighlightColor = cmd.HighlightColor;
        if (cmd.AiQuestion is not null) ann.AiQuestion = cmd.AiQuestion;
        await db.SaveChangesAsync(ct);
    }
}

public class DeleteAnnotationCommandHandler(IApplicationDbContext db)
{
    public async Task Handle(DeleteAnnotationCommand cmd, CancellationToken ct = default)
    {
        var ann = await db.BookAnnotations.FirstOrDefaultAsync(a => a.Id == cmd.Id && a.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("标注不存在");
        db.BookAnnotations.Remove(ann);
        await db.SaveChangesAsync(ct);
    }
}

public class AiAnalyzeTextCommandHandler(IApplicationDbContext db, IAiService aiService)
{
    public async Task<string> Handle(AiAnalyzeTextCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.BookId && b.TenantId == cmd.TenantId, ct)
            ?? throw new KeyNotFoundException("图书不存在");
        return await aiService.AnalyzeBookTextAsync(cmd.SelectedText, cmd.Question, book.Title, ct);
    }
}
