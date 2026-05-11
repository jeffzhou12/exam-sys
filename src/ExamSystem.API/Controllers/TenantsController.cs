using ExamSystem.Application.Tenants.Commands;
using ExamSystem.Application.Tenants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
[ApiController]
[Route("api/tenants")]
[Produces("application/json")]
public class TenantsController(
    GetTenantsQueryHandler getTenantsHandler,
    GetTenantByIdQueryHandler getTenantByIdHandler,
    CreateTenantCommandHandler createTenantHandler,
    UpdateTenantCommandHandler updateTenantHandler,
    ToggleTenantStatusCommandHandler toggleStatusHandler) : ControllerBase
{
    /// <summary>获取租户列表（分页）</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await getTenantsHandler.Handle(new GetTenantsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>获取租户详情</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await getTenantByIdHandler.Handle(new GetTenantByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>创建新租户</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = await createTenantHandler.Handle(
            new CreateTenantCommand(request.Name, request.ContactEmail, request.AiCallQuota),
            cancellationToken);

        return CreatedAtAction(nameof(GetTenant), new { id }, new { id });
    }

    /// <summary>更新租户信息</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTenant(
        Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateTenantHandler.Handle(
            new UpdateTenantCommand(id, request.Name, request.ContactEmail, request.AiCallQuota),
            cancellationToken);

        return NoContent();
    }

    /// <summary>启用 / 停用租户</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await toggleStatusHandler.Handle(new ToggleTenantStatusCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }
}

public record CreateTenantRequest(string Name, string ContactEmail, int AiCallQuota = 1000);
public record UpdateTenantRequest(string Name, string ContactEmail, int AiCallQuota);
public record ToggleStatusRequest(bool IsActive);

