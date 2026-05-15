using ExamSystem.Application.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>审计日志查询（仅超级管理员）</summary>
[Authorize(Roles = Roles.SuperAdmin)]
[ApiController]
[Route("api/audit-logs")]
[Produces("application/json")]
public class AuditLogsController(GetAuditLogsQueryHandler handler) : ControllerBase
{
    /// <summary>分页查询审计日志</summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid?    tenantId,
        [FromQuery] string?  username,
        [FromQuery] string?  action,
        [FromQuery] string?  entityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new GetAuditLogsQuery(tenantId, username, action, entityType, from, to, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
