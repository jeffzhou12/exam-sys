using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.ExamPapers.Commands;
using ExamSystem.Application.ExamPapers.Queries;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/exam-papers")]
[Produces("application/json")]
public class ExamPapersController(
    GetExamPapersQueryHandler getHandler,
    CreateExamPaperCommandHandler createHandler,
    PublishExamPaperCommandHandler publishHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>获取试卷列表（分页）</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetExamPapers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ExamStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result   = await getHandler.Handle(
            new GetExamPapersQuery(tenantId, page, pageSize, status), cancellationToken);
        return Ok(result);
    }

    /// <summary>创建试卷</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateExamPaper(
        [FromBody] CreateExamPaperRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var id       = await createHandler.Handle(
            new CreateExamPaperCommand(
                tenantId, request.Title, request.Description,
                request.TotalScore, request.DurationMinutes,
                request.StartTime, request.EndTime,
                request.AntiCheatingEnabled,
                request.Questions.Select(q => new ExamQuestionItem(q.QuestionId, q.Score, q.Order)).ToList()),
            cancellationToken);
        return CreatedAtAction(nameof(GetExamPapers), new { }, new { id });
    }

    /// <summary>发布试卷</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PublishExamPaper(
        Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        await publishHandler.Handle(new PublishExamPaperCommand(tenantId, id), cancellationToken);
        return NoContent();
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
