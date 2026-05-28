using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Commands;

public record AnalyzeExamResultCommand(Guid ExamPaperId, string StudentId);

public class AnalyzeExamResultCommandHandler(
    IApplicationDbContext context,
    IAiService aiService)
{
    public async Task<string> Handle(AnalyzeExamResultCommand cmd, CancellationToken ct = default)
    {
        var paper = await context.ExamPapers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == cmd.ExamPaperId, ct)
            ?? throw new KeyNotFoundException($"试卷 {cmd.ExamPaperId} 不存在。");

        var answers = await context.StudentAnswers
            .AsNoTracking()
            .Include(sa => sa.Question)
            .Where(sa => sa.ExamPaperId == cmd.ExamPaperId && sa.StudentId == cmd.StudentId)
            .ToListAsync(ct);

        if (answers.Count == 0)
            throw new InvalidOperationException("暂无答题记录，无法进行 AI 分析。");

        var totalScore = answers.Sum(a => a.Score ?? 0);
        var pct = paper.TotalScore > 0 ? (double)totalScore / paper.TotalScore * 100 : 0;

        // 取得分为0（含未批改）的题目摘要
        var wrongSummaries = answers
            .Where(a => a.Score == null || a.Score == 0)
            .Select(a =>
            {
                var content = a.Question.Content;
                var snippet = content.Length > 80 ? content[..80] + "…" : content;
                var kp = string.IsNullOrWhiteSpace(a.Question.KnowledgePoint) ? "未标注" : a.Question.KnowledgePoint;
                return $"- {snippet}（知识点：{kp}）";
            })
            .Take(15)
            .ToList();

        return await aiService.AnalyzeExamResultAsync(
            paper.Title, totalScore, paper.TotalScore, pct, wrongSummaries, ct);
    }
}
