using ExamSystem.Application.StudentAnswers.Commands;
using ExamSystem.Application.StudentAnswers.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/exam-papers/{examPaperId:guid}/answers")]
[Produces("application/json")]
public class StudentAnswersController(
    SubmitAnswersCommandHandler submitHandler,
    GradeWithAiCommandHandler gradeHandler,
    GetStudentResultQueryHandler resultHandler) : ControllerBase
{
    /// <summary>考生提交答案（客观题自动评分，简答题待 AI 评分）</summary>
    [HttpPost]
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
}

public record SubmitAnswersRequest(string StudentId, List<AnswerRequest> Answers);
public record AnswerRequest(Guid QuestionId, string Content);
