using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Commands;

public record UpdateExamPaperCommand(
    Guid TenantId,
    Guid ExamPaperId,
    string Title,
    string? Description,
    int TotalScore,
    int DurationMinutes,
    DateTime? StartTime,
    DateTime? EndTime,
    bool AntiCheatingEnabled,
    List<ExamQuestionItem> Questions);

public class UpdateExamPaperCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(UpdateExamPaperCommand command, CancellationToken cancellationToken = default)
    {
        var paper = await context.ExamPapers
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamPaperId && e.TenantId == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"试卷 {command.ExamPaperId} 不存在。");

        if (paper.Status != ExamStatus.Draft)
            throw new InvalidOperationException("只有草稿状态的试卷可以修改。");

        var questionsScore = command.Questions.Sum(q => q.Score);
        if (questionsScore != command.TotalScore)
            throw new InvalidOperationException(
                $"题目分值之和({questionsScore})与试卷总分({command.TotalScore})不一致。");

        var questionIds = command.Questions.Select(q => q.QuestionId).ToList();
        var validCount = await context.Questions
            .CountAsync(q => questionIds.Contains(q.Id) && q.TenantId == command.TenantId && q.IsActive,
                cancellationToken);

        if (validCount != questionIds.Count)
            throw new InvalidOperationException("部分题目不存在或不属于当前租户。");

        paper.Title = command.Title;
        paper.Description = command.Description;
        paper.TotalScore = command.TotalScore;
        paper.DurationMinutes = command.DurationMinutes;
        paper.StartTime = command.StartTime;
        paper.EndTime = command.EndTime;
        paper.AntiCheatingEnabled = command.AntiCheatingEnabled;
        paper.UpdatedAt = DateTime.UtcNow;

        // 替换题目列表
        paper.ExamQuestions.Clear();
        foreach (var q in command.Questions)
        {
            paper.ExamQuestions.Add(new ExamQuestion
            {
                QuestionId = q.QuestionId,
                Score = q.Score,
                Order = q.Order
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
