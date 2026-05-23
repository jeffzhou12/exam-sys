using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Application.Questions.Commands;
using ExamSystem.Application.Questions.Queries;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamSystem.API.Controllers;

[Authorize(Roles = Roles.AdminOrTeacher)]
[ApiController]
[Route("api/questions")]
[Produces("application/json")]
public class QuestionsController(
    GetQuestionsQueryHandler getQuestionsHandler,
    GetQuestionByIdQueryHandler getQuestionByIdHandler,
    CreateQuestionCommandHandler createQuestionHandler,
    UpdateQuestionCommandHandler updateQuestionHandler,
    DeleteQuestionCommandHandler deleteQuestionHandler,
    GenerateQuestionsWithAiCommandHandler generateQuestionsHandler,
    PreviewAiQuestionsCommandHandler previewAiHandler,
    BatchCreateQuestionsCommandHandler batchCreateHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>获取题目列表（分页，支持多维过滤）</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<QuestionDto>), 200)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] QuestionType? type = null,
        [FromQuery] int? difficulty = null,
        [FromQuery] string? knowledgePoint = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await getQuestionsHandler.Handle(
            new GetQuestionsQuery(tenantId, page, pageSize, type, difficulty, knowledgePoint),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>获取题目详情（含完整选项和答案）</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestionDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetQuestion(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await getQuestionByIdHandler.Handle(new GetQuestionByIdQuery(tenantId, id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>手动新建题目</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var id = await createQuestionHandler.Handle(
            new CreateQuestionCommand(
                tenantId ?? throw new InvalidOperationException("请先选择租户。"),
                request.Type, request.Content,
                request.Options, request.CorrectAnswer,
                request.Explanation, request.KnowledgePoint, request.Difficulty),
            cancellationToken);

        return CreatedAtAction(nameof(GetQuestion), new { id }, new { id });
    }

    /// <summary>更新题目</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateQuestion(
        Guid id,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        await updateQuestionHandler.Handle(
            new UpdateQuestionCommand(tenantId ?? throw new InvalidOperationException("请先选择租户。"),
                id, request.Type, request.Content,
                request.Options, request.CorrectAnswer, request.Explanation,
                request.KnowledgePoint, request.Difficulty),
            cancellationToken);
        return NoContent();
    }

    /// <summary>删除题目（软删除）</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        await deleteQuestionHandler.Handle(
            new DeleteQuestionCommand(tenantId ?? throw new InvalidOperationException("请先选择租户。"), id),
            cancellationToken);
        return NoContent();
    }

    /// <summary>使用 AI 自动生成题目</summary>
    [HttpPost("ai-generate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateWithAi(
        [FromBody] GenerateQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var typeConfigs = request.TypeConfigs is { Count: > 0 }
            ? request.TypeConfigs.Select(t => new AiTypeConfig(t.Type, t.Count, t.Difficulty, t.KnowledgePoint)).ToList()
            : [
                new AiTypeConfig(
                    request.QuestionType ?? throw new InvalidOperationException("请至少配置一种题型。"),
                    request.Count ?? 5,
                    request.Difficulty ?? 3,
                    request.KnowledgePoint)
            ];

        var count = await generateQuestionsHandler.Handle(
            new GenerateQuestionsWithAiCommand(
                tenantId ?? throw new InvalidOperationException("请先选择租户。"),
                request.KnowledgePoint,
                typeConfigs),
            cancellationToken);

        return Ok(new { generated = count });
    }

    /// <summary>AI 预览出题（不入库，返回生成结果供人工审核）</summary>
    [HttpPost("ai-preview")]
    [ProducesResponseType(typeof(List<AiQuestionPreviewDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PreviewWithAi(
        [FromBody] PreviewAiQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await previewAiHandler.Handle(
            new PreviewAiQuestionsCommand(
                request.KnowledgePoint,
                request.TypeConfigs.Select(t => new AiTypeConfig(t.Type, t.Count, t.Difficulty, t.KnowledgePoint)).ToList()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>批量创建题目（审核后保存 AI 生成题目）</summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(List<Guid>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BatchCreate(
        [FromBody] BatchCreateQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantService.GetCurrentTenantId()
            ?? throw new InvalidOperationException("请先选择租户。");
        var ids = await batchCreateHandler.Handle(
            new BatchCreateQuestionsCommand(
                tenantId,
                request.Questions.Select(q => new BatchQuestionItem(
                    q.Type, q.Content, q.Options, q.CorrectAnswer,
                    q.Explanation, q.KnowledgePoint, q.Difficulty)).ToList()),
            cancellationToken);
        return Ok(ids);
    }
}

public record CreateQuestionRequest(
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty = 1);

public record UpdateQuestionRequest(
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty = 1);

public record GenerateQuestionsRequest(
    string? KnowledgePoint,
    QuestionType? QuestionType,
    int? Count,
    int? Difficulty,
    List<AiTypeConfigRequest>? TypeConfigs);

public record PreviewAiQuestionsRequest(
    string KnowledgePoint,
    List<AiTypeConfigRequest> TypeConfigs);

public record AiTypeConfigRequest(QuestionType Type, int Count, int Difficulty = 3, string? KnowledgePoint = null);

public record BatchCreateQuestionsRequest(List<BatchQuestionItemRequest> Questions);

public record BatchQuestionItemRequest(
    QuestionType Type,
    string Content,
    List<string>? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty = 1);

