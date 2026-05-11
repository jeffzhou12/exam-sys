using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamSystem.Application.Questions.Queries;

public record GetQuestionByIdQuery(Guid TenantId, Guid QuestionId);

public record QuestionDetailDto(
    Guid Id,
    QuestionType Type,
    string Content,
    JsonDocument? Options,
    string CorrectAnswer,
    string? Explanation,
    string? KnowledgePoint,
    int Difficulty,
    bool IsAiGenerated,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class GetQuestionByIdQueryHandler(IApplicationDbContext context)
{
    public async Task<QuestionDetailDto?> Handle(
        GetQuestionByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await context.Questions
            .AsNoTracking()
            .Where(q => q.Id == query.QuestionId && q.TenantId == query.TenantId)
            .Select(q => new QuestionDetailDto(
                q.Id, q.Type, q.Content, q.Options, q.CorrectAnswer,
                q.Explanation, q.KnowledgePoint, q.Difficulty,
                q.IsAiGenerated, q.IsActive, q.CreatedAt, q.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
