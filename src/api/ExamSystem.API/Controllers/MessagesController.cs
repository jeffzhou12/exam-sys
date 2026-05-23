using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

/// <summary>站内信接口</summary>
[Authorize(Roles = Roles.All)]
[ApiController]
[Route("api/messages")]
[Produces("application/json")]
public class MessagesController(
    SendMessageCommandHandler sendHandler,
    GetMessagesQueryHandler getMessagesHandler,
    GetMessageThreadQueryHandler getThreadHandler,
    GetMessageQuestionsQueryHandler getQuestionsHandler,
    MarkMessageReadCommandHandler markReadHandler,
    GetTenantTeachersQueryHandler getTeachersHandler,
    ITenantService tenantService) : ControllerBase
{
    private Guid? TenantId => tenantService.GetCurrentTenantId();
    private bool IsSuperAdmin => User.IsInRole(Roles.SuperAdmin);
    private bool IsAnyAdmin => IsSuperAdmin || User.IsInRole(Roles.Admin);

    private Guid? UserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string UserName =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("unique_name")
        ?? "未知用户";

    /// <summary>管理员获取系统全部消息（不限当前用户）。SuperAdmin 不过滤租户，Admin 限定租户。</summary>
    [HttpGet("all")]
    [Authorize(Roles = Roles.SuperAdminOrAdmin)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin && TenantId is null)
            return BadRequest(new { error = "无法确定租户信息。" });

        var result = await getMessagesHandler.Handle(
            new GetMessagesQuery(IsSuperAdmin ? null : TenantId, UserId: null, IsInbox: true, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>获取收件箱消息。SuperAdmin 不过滤租户，普通用户限定租户。</summary>
    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();
        if (!IsSuperAdmin && TenantId is null)
            return BadRequest(new { error = "无法确定租户信息。" });

        var result = await getMessagesHandler.Handle(
            new GetMessagesQuery(IsSuperAdmin ? null : TenantId, UserId.Value, IsInbox: true, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>获取已发送消息。SuperAdmin 不过滤租户，普通用户限定租户。</summary>
    [HttpGet("sent")]
    public async Task<IActionResult> GetSent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();
        if (!IsSuperAdmin && TenantId is null)
            return BadRequest(new { error = "无法确定租户信息。" });

        var result = await getMessagesHandler.Handle(
            new GetMessagesQuery(IsSuperAdmin ? null : TenantId, UserId.Value, IsInbox: false, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>发送站内信。SuperAdmin 须在请求体中指定 TenantId。</summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();

        // 确定发送所属租户：SuperAdmin 从请求体中读取，否则从 TenantService 读取
        var tenantId = IsSuperAdmin ? request.TenantId : TenantId;
        if (tenantId is null)
            return BadRequest(new { error = IsSuperAdmin
                ? "超级管理员发送消息时须指定目标租户 (tenantId)。"
                : "无法确定租户信息。" });

        try
        {
            var id = await sendHandler.Handle(
                new SendMessageCommand(
                    tenantId.Value,
                    UserId.Value,
                    UserName,
                    request.RecipientId,
                    request.Subject,
                    request.Body,
                    request.AttachedQuestionIds,
                    request.AttachedExamPaperId,
                    request.ParentMessageId),
                cancellationToken);

            return Ok(new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>获取对话线程（根消息 + 全部回复）。</summary>
    [HttpGet("{id:guid}/thread")]
    public async Task<IActionResult> GetThread(
        Guid id, CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();
        if (!IsSuperAdmin && TenantId is null)
            return BadRequest(new { error = "无法确定租户信息。" });

        var thread = await getThreadHandler.Handle(
            id,
            IsSuperAdmin ? null : TenantId,
            UserId.Value,
            IsAnyAdmin,
            cancellationToken);
        if (thread is null) return Forbid();
        return Ok(thread);
    }

    /// <summary>获取消息关联的题目（学生可调用，无需 teacher 权限）。</summary>
    [HttpGet("{id:guid}/questions")]
    public async Task<IActionResult> GetMessageQuestions(
        Guid id, CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();
        if (!IsSuperAdmin && TenantId is null)
            return BadRequest(new { error = "无法确定租户信息。" });

        var questions = await getQuestionsHandler.Handle(
            id,
            IsSuperAdmin ? null : TenantId,
            UserId.Value,
            IsAnyAdmin,
            cancellationToken);
        if (questions is null) return Forbid();
        return Ok(questions);
    }

    /// <summary>标记消息为已读。SuperAdmin 不过滤租户。</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid id, CancellationToken cancellationToken = default)
    {
        if (UserId is null) return Unauthorized();

        await markReadHandler.Handle(
            new MarkMessageReadCommand(IsSuperAdmin ? null : TenantId, UserId.Value, id),
            cancellationToken);

        return NoContent();
    }

    /// <summary>获取教师列表。SuperAdmin 须通过 tenantId 参数指定租户。</summary>
    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = tenantId ?? TenantId;
        if (resolvedTenantId is null)
            return BadRequest(new { error = "请指定租户 (tenantId)。" });

        var teachers = await getTeachersHandler.Handle(resolvedTenantId.Value, cancellationToken);
        return Ok(teachers);
    }
}

public record SendMessageRequest(
    Guid RecipientId,
    string Subject,
    string Body,
    List<Guid>? AttachedQuestionIds = null,
    Guid? AttachedExamPaperId = null,
    Guid? TenantId = null,
    Guid? ParentMessageId = null);
