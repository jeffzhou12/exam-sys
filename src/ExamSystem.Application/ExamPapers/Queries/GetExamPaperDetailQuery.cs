using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Queries;

public record GetExamPaperDetailQuery(Guid TenantId, Guid ExamPaperId);

public record ExamPaperDetailDto(
    Guid Id,
    string Title,
    string? Description,
    int TotalScore,
    int DurationMinutes,
    ExamStatus Status,
    DateTime? StartTime,
    DateTime? EndTime,
    bool AntiCheatingEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ExamPaperQuestionDetailDto> Questions);

public record ExamPaperQuestionDetailDto(
    Guid QuestionId,
    QuestionType Type,
    string Content,
    int Score,
    int Order,
    string? KnowledgePoint,
    int Difficulty);

public class GetExamPaperDetailQueryHandler(IApplicationDbContext context)
{
    public async Task<ExamPaperDetailDto?> Handle(
        GetExamPaperDetailQuery query, CancellationToken cancellationToken = default)
    {
        var paper = await context.ExamPapers
            .AsNoTracking()
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == query.ExamPaperId && e.TenantId == query.TenantId, cancellationToken);

        if (paper is null)
            return null;

        var questions = paper.ExamQuestions
            .OrderBy(eq => eq.Order)
            .Select(eq => new ExamPaperQuestionDetailDto(
                eq.QuestionId,
                eq.Question.Type,
                eq.Question.Content,
                eq.Score,
                eq.Order,
                eq.Question.KnowledgePoint,
                eq.Question.Difficulty))
            .ToList();

        return new ExamPaperDetailDto(
            paper.Id, paper.Title, paper.Description,
            paper.TotalScore, paper.DurationMinutes, paper.Status,
            paper.StartTime, paper.EndTime, paper.AntiCheatingEnabled,
            paper.CreatedAt, paper.UpdatedAt, questions);
    }
}
