using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>媒体文件上传（图片封面等）</summary>
[Authorize]
[ApiController]
[Route("api/media")]
public class MediaController(IFileStorageService fileStorage) : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];

    /// <summary>上传图片（封面图等），返回可访问的 URL</summary>
    [HttpPost("image")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "请上传有效的图片文件" });

        if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = "只允许上传 JPG、PNG、WebP 或 GIF 格式图片" });

        await using var stream = file.OpenReadStream();
        var key = await fileStorage.SaveAsync(stream, file.FileName, "covers", ct);

        // S3：使用 1 年期预签名 URL；本地：使用 /uploads/ 静态文件路径
        var presignedUrl = await fileStorage.GetPresignedUrlAsync(key, 60 * 24 * 365);
        var url = presignedUrl ?? $"{Request.Scheme}://{Request.Host}/uploads/{key}";

        return Ok(new { url });
    }
}
