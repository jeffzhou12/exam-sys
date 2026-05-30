using ExamSystem.Application.AiConfigs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>AI 调用审计日志查询（仅超级管理员）</summary>
[Authorize(Roles = Roles.SuperAdmin)]
[ApiController]
[Route("api/ai-audit-logs")]
[Produces("application/json")]
public class AiAuditLogsController(GetAiAuditLogsQueryHandler handler) : ControllerBase
{
    /// <summary>分页查询 AI 调用审计日志</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AiAuditLogPageResult), 200)]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? operation,
        [FromQuery] bool? isSuccess,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new GetAiAuditLogsQuery(tenantId, operation, isSuccess, from, to, page, pageSize),
            cancellationToken);
        return Ok(result);
    }
}
