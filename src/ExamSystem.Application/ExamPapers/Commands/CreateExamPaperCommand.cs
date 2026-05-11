using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Commands;

public record CreateExamPaperCommand(
    Guid TenantId,
    string Title,
    string? Description,
    int TotalScore,
    int DurationMinutes,
    DateTime? StartTime,
    DateTime? EndTime,
    bool AntiCheatingEnabled,
    List<ExamQuestionItem> Questions);

public record ExamQuestionItem(Guid QuestionId, int Score, int Order);

public class CreateExamPaperCommandHandler(IApplicationDbContext context)
{
    public async Task<Guid> Handle(CreateExamPaperCommand command, CancellationToken cancellationToken = default)
    {
        // 校验总分与题目分值之和一致
        var questionsScore = command.Questions.Sum(q => q.Score);
        if (questionsScore != command.TotalScore)
            throw new InvalidOperationException(
                $"题目分值之和({questionsScore})与试卷总分({command.TotalScore})不一致。");

        // 校验题目属于本租户
        var questionIds = command.Questions.Select(q => q.QuestionId).ToList();
        var validCount = await context.Questions
            .CountAsync(q => questionIds.Contains(q.Id) && q.TenantId == command.TenantId && q.IsActive,
                cancellationToken);

        if (validCount != questionIds.Count)
            throw new InvalidOperationException("部分题目不存在或不属于当前租户。");

        var paper = new ExamPaper
        {
            TenantId         = command.TenantId,
            Title            = command.Title,
            Description      = command.Description,
            TotalScore       = command.TotalScore,
            DurationMinutes  = command.DurationMinutes,
            StartTime        = command.StartTime,
            EndTime          = command.EndTime,
            AntiCheatingEnabled = command.AntiCheatingEnabled,
            Status           = ExamStatus.Draft,
            ExamQuestions    = command.Questions.Select(q => new ExamQuestion
            {
                QuestionId = q.QuestionId,
                Score      = q.Score,
                Order      = q.Order
            }).ToList()
        };

        context.ExamPapers.Add(paper);
        await context.SaveChangesAsync(cancellationToken);
        return paper.Id;
    }
}
