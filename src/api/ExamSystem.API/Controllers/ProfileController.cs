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
    GetUserByIdQueryHandler getUserByIdHandler,
    UpdateUserCommandHandler updateUserHandler) : ControllerBase
{
    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await getUserByIdHandler.Handle(new GetUserByIdQuery(CurrentUserId.Value), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
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

        await updateUserHandler.Handle(new UpdateUserCommand(
            CurrentUserId.Value,
            request.Email,
            request.PhoneNumber,
            request.Nickname,
            request.AvatarUrl,
            request.Gender,
            request.Address,
            request.WeChatOpenId,
            request.WeChatUnionId), cancellationToken);

        return NoContent();
    }
}

public record UpdateProfileRequest(
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId);