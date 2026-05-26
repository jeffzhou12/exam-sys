using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.SmsTemplates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>
/// 短信模板管理接口。
/// - SuperAdmin 可管理系统级模板及任意租户模板
/// - Admin 仅可管理当前租户模板
/// </summary>
[Authorize(Roles = Roles.SuperAdminOrAdmin)]
[ApiController]
[Route("api/sms-templates")]
[Produces("application/json")]
public class SmsTemplatesController(
    GetSmsTemplatesQueryHandler getTemplatesHandler,
    GetSmsTemplateByIdQueryHandler getByIdHandler,
    CreateSmsTemplateCommandHandler createHandler,
    UpdateSmsTemplateCommandHandler updateHandler,
    DeleteSmsTemplateCommandHandler deleteHandler,
    ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<SmsTemplateDto>), 200)]
    public async Task<IActionResult> GetList([FromQuery] Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        var result = await getTemplatesHandler.Handle(new GetSmsTemplatesQuery(resolvedTenantId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SmsTemplateDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await getByIdHandler.Handle(new GetSmsTemplateByIdQuery(id), cancellationToken);
        if (result is null)
            return NotFound();

        if (!IsSuperAdmin() && result.TenantId != tenantService.GetCurrentTenantId())
            return Forbid();

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<IActionResult> Create([FromBody] CreateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = ResolveTenantId(request.TenantId);
        var id = await createHandler.Handle(new CreateSmsTemplateCommand(
            resolvedTenantId,
            request.Scene,
            request.Name,
            request.TemplateBody,
            request.IsEnabled,
            request.Priority,
            request.Description), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureOwnershipAsync(id, cancellationToken);
        await updateHandler.Handle(new UpdateSmsTemplateCommand(
            id,
            request.Scene,
            request.Name,
            request.TemplateBody,
            request.IsEnabled,
            request.Priority,
            request.Description), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureOwnershipAsync(id, cancellationToken);
        await deleteHandler.Handle(new DeleteSmsTemplateCommand(id), cancellationToken);
        return NoContent();
    }

    private bool IsSuperAdmin() => User.IsInRole(Roles.SuperAdmin);

    private Guid? ResolveTenantId(Guid? requestedTenantId)
        => IsSuperAdmin() ? requestedTenantId : tenantService.GetCurrentTenantId();

    private async Task EnsureOwnershipAsync(Guid templateId, CancellationToken ct)
    {
        if (IsSuperAdmin())
            return;

        var template = await getByIdHandler.Handle(new GetSmsTemplateByIdQuery(templateId), ct)
            ?? throw new KeyNotFoundException($"短信模板 {templateId} 不存在");

        if (template.TenantId != tenantService.GetCurrentTenantId())
            throw new UnauthorizedAccessException("无权操作其他租户的短信模板");
    }
}

public record CreateSmsTemplateRequest(
    Guid? TenantId,
    string Scene,
    string Name,
    string TemplateBody,
    bool IsEnabled = true,
    int Priority = 0,
    string? Description = null);

public record UpdateSmsTemplateRequest(
    string Scene,
    string Name,
    string TemplateBody,
    bool IsEnabled = true,
    int Priority = 0,
    string? Description = null);