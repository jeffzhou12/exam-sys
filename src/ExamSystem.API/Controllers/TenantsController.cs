using ExamSystem.Application.Tenants.Commands;
using ExamSystem.Application.Tenants.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Produces("application/json")]
public class TenantsController(
    GetTenantsQueryHandler getTenantsHandler,
    CreateTenantCommandHandler createTenantHandler) : ControllerBase
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

        return CreatedAtAction(nameof(GetTenants), new { }, new { id });
    }
}

public record CreateTenantRequest(string Name, string ContactEmail, int AiCallQuota = 1000);
