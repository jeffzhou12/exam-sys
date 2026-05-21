using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>媒体文件上传（图片封面等）</summary>
[Authorize]
[ApiController]
[Route("api/media")]
public class MediaController(IFileStorageFactory storageFactory) : ControllerBase
{
    private IFileStorageService MediaStorage => storageFactory.GetStorage("Media");
    private static readonly HashSet<string> AllowedImageTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];

    /// <summary>上传图片（封面图等），返回通过本服务代理访问的 URL（避免浏览器直接跨域访问 S3）</summary>
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
        var key = await MediaStorage.SaveAsync(stream, file.FileName, "covers", ct);

        // 统一返回经 API 代理的访问地址，前端直接使用此 URL 显示图片
        // 对各路径段单独编码，保留 / 分隔符，避免 %2F 在路由中无法自动解码
        var encodedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        var url = $"{Request.Scheme}://{Request.Host}/api/media/image/{encodedKey}";
        return Ok(new { url, key });
    }

    /// <summary>通过 API 代理读取图片（S3 或本地，统一入口，无跨域问题）</summary>
    [HttpGet("image/{*key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest();

        // 兼容旧 URL 中 %2F 未被路由解码的情况
        key = Uri.UnescapeDataString(key);

        try
        {
            var stream = await MediaStorage.GetStreamAsync(key, ct);
            var ext = Path.GetExtension(key).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".webp"           => "image/webp",
                ".gif"            => "image/gif",
                _                 => "application/octet-stream"
            };
            return File(stream, contentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
