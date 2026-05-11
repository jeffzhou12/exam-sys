using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Questions.Commands;
using ExamSystem.Application.Questions.Queries;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/questions")]
[Produces("application/json")]
public class QuestionsController(
    GetQuestionsQueryHandler getQuestionsHandler,
    CreateQuestionCommandHandler createQuestionHandler,
    GenerateQuestionsWithAiCommandHandler generateQuestionsHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>获取题目列表（分页，支持多维过滤）</summary>
    [HttpGet]
    [ProducesResponseType(200)]
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
                tenantId, request.Type, request.Content,
                request.Options, request.CorrectAnswer,
                request.Explanation, request.KnowledgePoint, request.Difficulty),
            cancellationToken);

        return CreatedAtAction(nameof(GetQuestions), new { }, new { id });
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
        var count = await generateQuestionsHandler.Handle(
            new GenerateQuestionsWithAiCommand(tenantId, request.KnowledgePoint, request.QuestionType, request.Count),
            cancellationToken);

        return Ok(new { generated = count });
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

public record GenerateQuestionsRequest(
    string KnowledgePoint,
    QuestionType QuestionType,
    int Count = 5);
