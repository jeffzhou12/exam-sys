using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.ExamPapers.Commands;

public record CancelExamPaperCommand(Guid TenantId, Guid ExamPaperId);

public class CancelExamPaperCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(CancelExamPaperCommand command, CancellationToken cancellationToken = default)
    {
        var paper = await context.ExamPapers
            .FirstOrDefaultAsync(e => e.Id == command.ExamPaperId && e.TenantId == command.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"试卷 {command.ExamPaperId} 不存在。");

        if (paper.Status == ExamStatus.Ended || paper.Status == ExamStatus.Cancelled)
            throw new InvalidOperationException("试卷已结束或已取消，无法再次操作。");

        paper.Status = ExamStatus.Cancelled;
        paper.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
