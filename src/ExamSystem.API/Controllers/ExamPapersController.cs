using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.ExamPapers.Commands;
using ExamSystem.Application.ExamPapers.Queries;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/exam-papers")]
[Produces("application/json")]
public class ExamPapersController(
    GetExamPapersQueryHandler getHandler,
    GetExamPaperDetailQueryHandler getDetailHandler,
    CreateExamPaperCommandHandler createHandler,
    UpdateExamPaperCommandHandler updateHandler,
    PublishExamPaperCommandHandler publishHandler,
    CancelExamPaperCommandHandler cancelHandler,
    GetExamResultsQueryHandler resultsHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>获取试卷列表（分页）</summary>
    [HttpGet]
    [Authorize(Roles = Roles.All)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetExamPapers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ExamStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await getHandler.Handle(
            new GetExamPapersQuery(tenantId, page, pageSize, status), cancellationToken);
        return Ok(result);
    }

    /// <summary>获取试卷详情（含题目列表）</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.All)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetExamPaper(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await getDetailHandler.Handle(new GetExamPaperDetailQuery(tenantId, id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>创建试卷</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateExamPaper(
        [FromBody] CreateExamPaperRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId is null) return BadRequest(new { error = "请先通过租户切换器选择一个租户。" });
        var id = await createHandler.Handle(
            new CreateExamPaperCommand(
                tenantId.Value, request.Title, request.Description,
                request.TotalScore, request.DurationMinutes,
                request.StartTime, request.EndTime,
                request.AntiCheatingEnabled,
                request.Questions.Select(q => new ExamQuestionItem(q.QuestionId, q.Score, q.Order)).ToList()),
            cancellationToken);
        return CreatedAtAction(nameof(GetExamPaper), new { id }, new { id });
    }

    /// <summary>更新试卷（仅草稿状态可编辑）</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateExamPaper(
        Guid id,
        [FromBody] CreateExamPaperRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId is null) return BadRequest(new { error = "请先通过租户切换器选择一个租户。" });
        await updateHandler.Handle(
            new UpdateExamPaperCommand(
                tenantId.Value, id, request.Title, request.Description,
                request.TotalScore, request.DurationMinutes,
                request.StartTime, request.EndTime,
                request.AntiCheatingEnabled,
                request.Questions.Select(q => new ExamQuestionItem(q.QuestionId, q.Score, q.Order)).ToList()),
            cancellationToken);
        return NoContent();
    }

    /// <summary>发布试卷</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PublishExamPaper(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId is null) return BadRequest(new { error = "请先通过租户切换器选择一个租户。" });
        await publishHandler.Handle(new PublishExamPaperCommand(tenantId.Value, id), cancellationToken);
        return NoContent();
    }

    /// <summary>取消试卷</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelExamPaper(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId is null) return BadRequest(new { error = "请先通过租户切换器选择一个租户。" });
        await cancelHandler.Handle(new CancelExamPaperCommand(tenantId.Value, id), cancellationToken);
        return NoContent();
    }

    /// <summary>查看考试成绩汇总（所有学生）</summary>
    [HttpGet("{id:guid}/results")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetExamResults(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await resultsHandler.Handle(new GetExamResultsQuery(tenantId, id, page, pageSize), cancellationToken);
        return Ok(result);
    }
}

public record CreateExamPaperRequest(
    string Title,
    string? Description,
    int TotalScore,
    int DurationMinutes,
    DateTime? StartTime,
    DateTime? EndTime,
    bool AntiCheatingEnabled,
    List<ExamPaperQuestionRequest> Questions);

public record ExamPaperQuestionRequest(Guid QuestionId, int Score, int Order);

