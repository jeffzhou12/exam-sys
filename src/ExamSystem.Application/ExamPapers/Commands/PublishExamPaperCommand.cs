using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Commands;

public record PublishExamPaperCommand(Guid TenantId, Guid ExamPaperId);

public class PublishExamPaperCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(PublishExamPaperCommand command, CancellationToken cancellationToken = default)
    {
        var paper = await context.ExamPapers
            .FirstOrDefaultAsync(e => e.Id == command.ExamPaperId && e.TenantId == command.TenantId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"试卷 {command.ExamPaperId} 不存在。");

        if (paper.Status != ExamStatus.Draft)
            throw new InvalidOperationException("只有草稿状态的试卷才能发布。");

        if (!await context.ExamQuestions.AnyAsync(
            eq => eq.ExamPaperId == paper.Id, cancellationToken))
            throw new InvalidOperationException("试卷中没有题目，无法发布。");

        paper.Status = ExamStatus.Published;
        await context.SaveChangesAsync(cancellationToken);
    }
}
