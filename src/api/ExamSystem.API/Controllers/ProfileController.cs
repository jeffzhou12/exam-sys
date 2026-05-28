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
    UpdateMyProfileCommandHandler updateMyProfileHandler) : ControllerBase
{
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
}

public record UpdateProfileRequest(
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? EducationLevel,
    List<string>? InterestedSubjects);
