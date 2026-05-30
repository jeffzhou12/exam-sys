using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Users.Commands;
using ExamSystem.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[Authorize]
public class ProfileController(
    GetMyProfileQueryHandler getMyProfileHandler,
    UpdateMyProfileCommandHandler updateMyProfileHandler,
    ChangeMyPasswordCommandHandler changePasswordHandler,
    ChangeMyPhoneCommandHandler changePhoneHandler,
    IFileStorageFactory storageFactory) : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private IFileStorageService AvatarStorage => storageFactory.GetStorage("Media");

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    [ProducesResponseType(typeof(MyProfileDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await getMyProfileHandler.Handle(new GetMyProfileQuery(CurrentUserId.Value), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        await updateMyProfileHandler.Handle(new UpdateMyProfileCommand(
            CurrentUserId.Value,
            request.Nickname,
            request.AvatarUrl,
            request.Gender,
            request.Address,
            request.EducationLevel,
            request.InterestedSubjects), cancellationToken);

        return NoContent();
    }

    /// <summary>上传当前用户头像，返回可访问的图片 URL</summary>
    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "请上传有效的图片文件" });
        if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = "只允许上传 JPG、PNG、WebP 或 GIF 格式图片" });

        await using var stream = file.OpenReadStream();
        var key = await AvatarStorage.SaveAsync(stream, file.FileName, "avatars", ct);
        var encodedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        var url = $"/api/media/image/{encodedKey}";

        // 同步写入用户 avatarUrl 字段
        await updateMyProfileHandler.Handle(
            new UpdateMyProfileCommand(CurrentUserId.Value, null, url, null, null, null, null), ct);

        return Ok(new { url, key });
    }

    /// <summary>换绑手机号（需先通过 /auth/send-code 获取验证码）</summary>
    [HttpPost("change-phone")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePhone(
        [FromBody] ChangePhoneRequest request,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        var (ok, error) = await changePhoneHandler.Handle(
            new ChangeMyPhoneCommand(CurrentUserId.Value, request.NewPhone, request.Code), ct);
        return ok ? NoContent() : BadRequest(new { error });
    }

    /// <summary>修改密码（需提供当前密码）</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        var (ok, error) = await changePasswordHandler.Handle(
            new ChangeMyPasswordCommand(CurrentUserId.Value, request.OldPassword, request.NewPassword), ct);
        return ok ? NoContent() : BadRequest(new { error });
    }
}

public record UpdateProfileRequest(
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? EducationLevel,
    List<string>? InterestedSubjects);

public record ChangePhoneRequest(string NewPhone, string Code);

public record ChangePasswordRequest(string OldPassword, string NewPassword);
