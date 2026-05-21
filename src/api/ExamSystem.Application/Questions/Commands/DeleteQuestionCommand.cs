using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Questions.Commands;

public record DeleteQuestionCommand(Guid TenantId, Guid QuestionId);

public class DeleteQuestionCommandHandler(
    IApplicationDbContext context,
    ICacheService cacheService)
{
    public async Task Handle(DeleteQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = await context.Questions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId && q.TenantId == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"题目 {command.QuestionId} 不存在。");

        // 检查是否被试卷引用（已发布/进行中的试卷）
        var inUse = await context.ExamQuestions
            .AnyAsync(eq => eq.QuestionId == command.QuestionId
                && (eq.ExamPaper.Status == Domain.Enums.ExamStatus.Published
                    || eq.ExamPaper.Status == Domain.Enums.ExamStatus.InProgress),
                cancellationToken);

        if (inUse)
            throw new InvalidOperationException("该题目已被发布或进行中的试卷引用，无法删除。");

        question.IsActive = false;
        question.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveByPrefixAsync($"questions:{command.TenantId}:", cancellationToken);
    }
}
