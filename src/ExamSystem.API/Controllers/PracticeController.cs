using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Practice;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

/// <summary>学生在线练习接口（所有已认证角色均可访问）</summary>
[Authorize(Roles = Roles.All)]
[ApiController]
[Route("api/practice")]
[Produces("application/json")]
public class PracticeController(
    GetPracticeQuestionsQueryHandler getQuestionsHandler,
    SubmitPracticeCommandHandler submitHandler,
    GetSimilarQuestionsQueryHandler similarHandler,
    ExplainQuestionCommandHandler explainHandler,
    ITenantService tenantService) : ControllerBase
{
    private Guid? TenantId => tenantService.GetCurrentTenantId();

    /// <summary>随机抽取练习题目（不含正确答案）</summary>
    [HttpGet("questions")]
    public async Task<IActionResult> GetPracticeQuestions(
        [FromQuery] int count = 10,
        [FromQuery] QuestionType? type = null,
        [FromQuery] int? difficulty = null,
        [FromQuery] string? knowledgePoint = null,
        CancellationToken cancellationToken = default)
    {
        if (TenantId is null)
            return BadRequest(new { error = "无法确定租户，请检查登录状态。" });

        var result = await getQuestionsHandler.Handle(
            new GetPracticeQuestionsQuery(TenantId.Value, count, type, difficulty, knowledgePoint),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>提交练习答案，返回自动批改结果（含正确答案和解析）</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitPractice(
        [FromBody] SubmitPracticeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (TenantId is null)
            return BadRequest(new { error = "无法确定租户，请检查登录状态。" });

        var result = await submitHandler.Handle(
            new SubmitPracticeCommand(
                TenantId.Value,
                request.Answers.Select(a => new PracticeAnswerItem(a.QuestionId, a.Answer)).ToList()),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>获取与指定题目相似的题目（按知识点 + 难度匹配）</summary>
    [HttpGet("questions/{questionId:guid}/similar")]
    public async Task<IActionResult> GetSimilarQuestions(
        Guid questionId,
        [FromQuery] string? knowledgePoint = null,
        [FromQuery] int difficulty = 1,
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        if (TenantId is null)
            return BadRequest(new { error = "无法确定租户。" });

        var result = await similarHandler.Handle(
            new GetSimilarQuestionsQuery(TenantId.Value, questionId, knowledgePoint, difficulty, count),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>AI 详解指定题目</summary>
    [HttpPost("questions/{questionId:guid}/explain")]
    public async Task<IActionResult> ExplainQuestion(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        if (TenantId is null)
            return BadRequest(new { error = "无法确定租户。" });

        try
        {
            var explanation = await explainHandler.Handle(
                new ExplainQuestionCommand(TenantId.Value, questionId),
                cancellationToken);
            return Ok(new { explanation });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public record SubmitAnswerItem(Guid QuestionId, string Answer);
public record SubmitPracticeRequest(List<SubmitAnswerItem> Answers);
