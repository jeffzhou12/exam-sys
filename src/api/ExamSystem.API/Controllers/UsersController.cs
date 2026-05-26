using ExamSystem.Application.Common.Models;
using ExamSystem.Application.Users.Commands;
using ExamSystem.Application.Users.Queries;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = Roles.SuperAdminOrAdmin)]
public class UsersController(
    GetUsersQueryHandler getUsersHandler,
    GetUserByIdQueryHandler getUserByIdHandler,
    CreateUserCommandHandler createUserHandler,
    UpdateUserCommandHandler updateUserHandler,
    ToggleUserStatusCommandHandler toggleStatusHandler,
    AdminResetPasswordCommandHandler resetPasswordHandler) : ControllerBase
{
    /// <summary>获取用户列表（SuperAdmin 可查所有租户，Admin 只能查自己租户）</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // 普通管理员强制限定为自己的租户，防止越权查询
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(userRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var tenantClaim = User.FindFirstValue("tenant_id");
            tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;
        }

        var result = await getUsersHandler.Handle(
            new GetUsersQuery(tenantId, page, pageSize, role, isActive, search),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>获取用户详情</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await getUserByIdHandler.Handle(new GetUserByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>创建用户（管理员指定角色创建）</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = await createUserHandler.Handle(
            new CreateUserCommand(
                request.TenantId,
                request.Username,
                request.Password,
                request.Email,
                request.PhoneNumber,
                request.Nickname,
                request.AvatarUrl,
                request.Gender,
                request.Address,
                request.WeChatOpenId,
                request.WeChatUnionId,
                request.Role),
            cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id }, new { id });
    }

    /// <summary>更新用户信息（邮箱、角色，SuperAdmin 可修改所属租户）</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // 仅 SuperAdmin 允许修改所属租户
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        var tenantId = string.Equals(userRole, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            ? request.TenantId
            : null;
        await updateUserHandler.Handle(new UpdateUserCommand(
            id,
            request.Email,
            request.PhoneNumber,
            request.Nickname,
            request.AvatarUrl,
            request.Gender,
            request.Address,
            request.WeChatOpenId,
            request.WeChatUnionId,
            request.Role,
            tenantId), cancellationToken);
        return NoContent();
    }

    /// <summary>启用 / 停用用户账号</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await toggleStatusHandler.Handle(new ToggleUserStatusCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }

    /// <summary>管理员重置用户密码</summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await resetPasswordHandler.Handle(new AdminResetPasswordCommand(id, request.NewPassword), cancellationToken);
        return NoContent();
    }
}

public record CreateUserRequest(
    Guid? TenantId,
    string Username,
    string Password,
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    UserRole Role);
public record UpdateUserRequest(
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    UserRole? Role,
    Guid? TenantId = null);
public record AdminResetPasswordRequest(string NewPassword);
