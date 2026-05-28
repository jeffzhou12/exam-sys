using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Favorites;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

/// <summary>用户收藏接口</summary>
[Authorize(Roles = Roles.All)]
[ApiController]
[Route("api/favorites")]
[Produces("application/json")]
public class FavoritesController(
    ToggleFavoriteCommandHandler toggleHandler,
    CheckFavoriteQueryHandler checkHandler,
    GetFavoritesQueryHandler getHandler,
    ITenantService tenantService) : ControllerBase
{
    private Guid? TenantId => tenantService.GetCurrentTenantId();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>切换收藏状态（已收藏则取消，未收藏则添加）</summary>
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(
        [FromBody] ToggleFavoriteRequest req,
        CancellationToken ct = default)
    {
        if (TenantId is null || UserId is null)
            return Unauthorized();

        var isFavorited = await toggleHandler.Handle(
            new ToggleFavoriteCommand(TenantId.Value, UserId,
                (FavoriteTargetType)req.TargetType, req.TargetId), ct);

        return Ok(new { isFavorited });
    }

    /// <summary>检查某对象是否已收藏</summary>
    [HttpGet("check")]
    public async Task<IActionResult> Check(
        [FromQuery] int targetType,
        [FromQuery] Guid targetId,
        CancellationToken ct = default)
    {
        if (TenantId is null || UserId is null)
            return Unauthorized();

        var isFavorited = await checkHandler.Handle(
            new CheckFavoriteQuery(TenantId.Value, UserId,
                (FavoriteTargetType)targetType, targetId), ct);

        return Ok(new { isFavorited });
    }

    /// <summary>获取收藏列表（按类型分页）</summary>
    [HttpGet]
    public async Task<IActionResult> GetFavorites(
        [FromQuery] int targetType = 1,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (TenantId is null || UserId is null)
            return Unauthorized();

        var (items, total) = await getHandler.Handle(
            new GetFavoritesQuery(TenantId.Value, UserId,
                (FavoriteTargetType)targetType, page, pageSize), ct);

        return Ok(new { items, total, page, pageSize });
    }
}

public record ToggleFavoriteRequest(int TargetType, Guid TargetId);
