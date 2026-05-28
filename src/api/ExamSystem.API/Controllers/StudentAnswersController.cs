using ExamSystem.Application.StudentAnswers.Commands;
using ExamSystem.Application.StudentAnswers.Queries;
using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/exam-papers/{examPaperId:guid}/answers")]
[Produces("application/json")]
public class StudentAnswersController(
    SubmitAnswersCommandHandler submitHandler,
    GradeWithAiCommandHandler gradeHandler,
    GetStudentResultQueryHandler resultHandler,
    ManualGradeCommandHandler manualGradeHandler,
    GetStudentExamsQueryHandler studentExamsHandler,
    AnalyzeExamResultCommandHandler analyzeHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>考生提交答案（客观题自动评分，简答题待 AI 评分）</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitAnswers(
        Guid examPaperId,
        [FromBody] SubmitAnswersRequest request,
        CancellationToken cancellationToken = default)
    {
        await submitHandler.Handle(
            new SubmitAnswersCommand(
                examPaperId,
                request.StudentId,
                request.Answers.Select(a => new AnswerItem(a.QuestionId, a.Content)).ToList()),
            cancellationToken);
        return NoContent();
    }

    /// <summary>对指定考生的简答题触发 AI 评分</summary>
    [HttpPost("{studentId}/grade-ai")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GradeWithAi(
        Guid examPaperId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var result = await gradeHandler.Handle(
            new GradeWithAiCommand(examPaperId, studentId), cancellationToken);
        return Ok(new { result.GradedCount, result.TotalTokensUsed });
    }

    /// <summary>查询考生成绩报告</summary>
    [HttpGet("{studentId}")]
    [Authorize(Roles = Roles.All)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStudentResult(
        Guid examPaperId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var result = await resultHandler.Handle(
            new GetStudentResultQuery(examPaperId, studentId), cancellationToken);
        return Ok(result);
    }

    /// <summary>手动评分（教师/管理员批改简答题）</summary>
    [HttpPatch("items/{answerId:guid}/grade")]
    [Authorize(Roles = Roles.AdminOrTeacher)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ManualGrade(
        Guid answerId,
        [FromBody] ManualGradeRequest request,
        CancellationToken cancellationToken = default)
    {
        await manualGradeHandler.Handle(
            new ManualGradeCommand(answerId, request.Score, request.Feedback),
            cancellationToken);
        return NoContent();
    }

    /// <summary>查询当前学生参加过的所有考试及成绩（前台専用）</summary>
    [HttpGet("~/api/student/my-results")]
    [Authorize(Roles = Roles.Student)]
    [ProducesResponseType(typeof(List<StudentExamSummaryDto>), 200)]
    public async Task<IActionResult> GetMyResults(CancellationToken cancellationToken = default)
    {
        var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await studentExamsHandler.Handle(
            new GetStudentExamsQuery(studentId, tenantId), cancellationToken);
        return Ok(result);
    }

    /// <summary>AI 智能分析本次考试成绩（前台）</summary>
    [HttpPost("{studentId}/analyze")]
    [Authorize(Roles = Roles.All)]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<IActionResult> AnalyzeResult(
        Guid examPaperId, string studentId, CancellationToken cancellationToken = default)
    {
        var analysis = await analyzeHandler.Handle(
            new AnalyzeExamResultCommand(examPaperId, studentId), cancellationToken);
        return Ok(new { analysis });
    }
}

public record SubmitAnswersRequest(string StudentId, List<AnswerRequest> Answers);
public record AnswerRequest(Guid QuestionId, string Content);
public record ManualGradeRequest(int Score, string? Feedback);

