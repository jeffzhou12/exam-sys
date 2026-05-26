using ExamSystem.Application.Books;
using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/books")]
[Produces("application/json")]
public class BooksController(
    GetBooksQueryHandler getBooksHandler,
    GetBookByIdQueryHandler getBookByIdHandler,
    CreateBookCommandHandler createBookHandler,
    UpdateBookCommandHandler updateBookHandler,
    UploadBookPdfCommandHandler uploadPdfHandler,
    DeleteBookCommandHandler deleteBookHandler,
    GetBookAnnotationsQueryHandler getAnnotationsHandler,
    CreateAnnotationCommandHandler createAnnotationHandler,
    UpdateAnnotationCommandHandler updateAnnotationHandler,
    DeleteAnnotationCommandHandler deleteAnnotationHandler,
    AiAnalyzeTextCommandHandler aiAnalyzeHandler,
    ITenantService tenantService,
    IFileStorageFactory fileStorageFactory) : ControllerBase
{
    private IFileStorageService BooksStorage => fileStorageFactory.GetStorage("Books");
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private string CurrentUserName =>
        User.FindFirstValue(ClaimTypes.Name) ?? "未知用户";

    // ─── 图书管理（管理员/教师） ───────────────────────────────────────────────

    /// <summary>获取图书列表（分页，支持多维过滤）</summary>
    [HttpGet]
    [AllowAnonymous]  // 允许匿名浏览，无租户头时返回全量图书
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? category,
        [FromQuery] string? tag,
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // 允许无租户头（portal 图书列表公开浏览），不传租户时返回全量
        var tenantId = tenantService.TryGetCurrentTenantId();
        var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";

        var result = await getBooksHandler.Handle(
            new GetBooksQuery(tenantId, category, tag, keyword, isActive, page, pageSize, mediaBaseUrl), ct);
        return Ok(result);
    }

    /// <summary>获取图书详情</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken ct = default)
    {
        // SuperAdmin 未选租户时 tenantId 为 null，允许按 ID 跨租户访问
        var tenantId = tenantService.GetCurrentTenantId();
        var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";

        var book = await getBookByIdHandler.Handle(new GetBookByIdQuery(id, tenantId, mediaBaseUrl), ct);
        return book is null ? NotFound() : Ok(book);
    }

    /// <summary>创建图书（仅管理员/教师）</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest req, CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("缺少租户信息");

        var id = await createBookHandler.Handle(new CreateBookCommand(
            tenantId, CurrentUserId, CurrentUserName,
            req.Title, req.Author, req.Publisher, req.Description,
            req.CoverImageUrl, req.Category, req.Tags, req.PublishYear, req.Isbn), ct);

        return CreatedAtAction(nameof(GetBook), new { id }, new { id });
    }

    /// <summary>更新图书信息（仅管理员/教师）</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] UpdateBookRequest req, CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("缺少租户信息");

        await updateBookHandler.Handle(new UpdateBookCommand(
            id, tenantId, req.Title, req.Author, req.Publisher, req.Description,
            req.CoverImageUrl, req.Category, req.Tags, req.PublishYear, req.Isbn, req.IsActive), ct);
        return NoContent();
    }

    /// <summary>上传 PDF 文件（仅管理员/教师）</summary>
    [HttpPost("{id:guid}/pdf")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [RequestSizeLimit(200 * 1024 * 1024)] // 200 MB
    public async Task<IActionResult> UploadPdf(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "请上传有效的 PDF 文件" });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "只允许上传 PDF 格式文件" });

        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("缺少租户信息");

        await using var stream = file.OpenReadStream();
        var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
        await uploadPdfHandler.Handle(
            new UploadBookPdfCommand(id, tenantId, stream, file.FileName, file.Length, mediaBaseUrl), ct);
        return NoContent();
    }

    /// <summary>获取 PDF 文件流（所有已认证用户）</summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct = default)
    {
        // SuperAdmin 未选租户时 tenantId 为 null，允许按 ID 跨租户访问
        var tenantId = tenantService.GetCurrentTenantId();

        var book = await getBookByIdHandler.Handle(new GetBookByIdQuery(id, tenantId), ct);
        if (book is null) return NotFound();
        if (!book.HasPdf) return NotFound(new { error = "该图书暂未上传 PDF 文件" });

        var key = await GetPdfKeyAsync(id, tenantId ?? Guid.Empty, ct);
        if (string.IsNullOrEmpty(key))
            return NotFound(new { error = "PDF 文件路径无效" });

        // 始终通过 API 代理流式输出，避免浏览器直接跨域访问 S3 产生 CORS 问题。
        // 对于需要鉴权的内容，代理模式也更安全（S3 URL 不暴露给客户端）。
        var stream = await BooksStorage.GetStreamAsync(key, ct);
        return File(stream, "application/pdf", enableRangeProcessing: true);
    }

    /// <summary>删除图书（仅管理员）</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.SuperAdminOrAdmin)]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("缺少租户信息");

        await deleteBookHandler.Handle(new DeleteBookCommand(id, tenantId), ct);
        return NoContent();
    }

    // ─── 标注 ─────────────────────────────────────────────────────────────────

    /// <summary>获取当前用户对该图书的所有标注</summary>
    [HttpGet("{bookId:guid}/annotations")]
    public async Task<IActionResult> GetAnnotations(Guid bookId, CancellationToken ct = default)
    {
        var items = await getAnnotationsHandler.Handle(
            new GetBookAnnotationsQuery(bookId, CurrentUserId), ct);
        return Ok(items);
    }

    /// <summary>创建标注（书签/备注/AI问答）</summary>
    [HttpPost("{bookId:guid}/annotations")]
    public async Task<IActionResult> CreateAnnotation(
        Guid bookId, [FromBody] CreateAnnotationRequest req, CancellationToken ct = default)
    {
        var ann = await createAnnotationHandler.Handle(new CreateAnnotationCommand(
            bookId, CurrentUserId, CurrentUserName,
            req.PageNumber, req.SelectedText, req.Note, req.AnnotationType,
            req.AiQuestion, req.PositionJson, req.HighlightColor ?? "#FFEB3B"), ct);
        return Ok(ann);
    }

    /// <summary>更新标注备注</summary>
    [HttpPut("{bookId:guid}/annotations/{annotationId:guid}")]
    public async Task<IActionResult> UpdateAnnotation(
        Guid annotationId, [FromBody] UpdateAnnotationRequest req, CancellationToken ct = default)
    {
        await updateAnnotationHandler.Handle(
            new UpdateAnnotationCommand(annotationId, CurrentUserId, req.Note, req.AiQuestion, req.HighlightColor ?? "#FFEB3B"), ct);
        return NoContent();
    }

    /// <summary>删除标注</summary>
    [HttpDelete("{bookId:guid}/annotations/{annotationId:guid}")]
    public async Task<IActionResult> DeleteAnnotation(
        Guid annotationId, CancellationToken ct = default)
    {
        await deleteAnnotationHandler.Handle(new DeleteAnnotationCommand(annotationId, CurrentUserId), ct);
        return NoContent();
    }

    /// <summary>框选文字 + AI 提问（即时，不保存标注）</summary>
    [HttpPost("{bookId:guid}/ai-analyze")]
    public async Task<IActionResult> AiAnalyze(
        Guid bookId, [FromBody] AiAnalyzeRequest req, CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("缺少租户信息");

        var answer = await aiAnalyzeHandler.Handle(
            new AiAnalyzeTextCommand(bookId, tenantId, req.SelectedText, req.Question, req.ImageBase64), ct);
        return Ok(new { answer });
    }

    // ─── 辅助 ─────────────────────────────────────────────────────────────────

    private async Task<string> GetPdfKeyAsync(Guid bookId, Guid tenantId, CancellationToken ct)
    {
        // 直接从 DbContext 取原始存储键（BookDto 不暴露此字段）
        var db = HttpContext.RequestServices
            .GetRequiredService<Application.Common.Interfaces.IApplicationDbContext>();
        var book = await db.Books.FindAsync([bookId], ct);
        return book?.PdfFilePath ?? string.Empty;
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public record CreateBookRequest(
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

public record UpdateBookRequest(
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

public record CreateAnnotationRequest(
    int PageNumber,
    string? SelectedText,
    string? Note,
    int AnnotationType,
    string? AiQuestion,
    string? PositionJson,
    string? HighlightColor
);

public record UpdateAnnotationRequest(string? Note, string? AiQuestion, string? HighlightColor);

public record AiAnalyzeRequest(string SelectedText, string Question, string? ImageBase64 = null);
