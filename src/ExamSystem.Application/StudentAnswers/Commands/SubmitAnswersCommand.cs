using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.StudentAnswers.Commands;

public record SubmitAnswersCommand(
    Guid ExamPaperId,
    string StudentId,
    List<AnswerItem> Answers);

public record AnswerItem(Guid QuestionId, string Content);

public class SubmitAnswersCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(SubmitAnswersCommand command, CancellationToken cancellationToken = default)
    {
        // 校验试卷状态
        var paper = await context.ExamPapers
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamPaperId, cancellationToken)
            ?? throw new KeyNotFoundException($"试卷 {command.ExamPaperId} 不存在。");

        if (paper.Status is not (ExamStatus.Published or ExamStatus.InProgress))
            throw new InvalidOperationException("试卷当前不在可作答状态。");

        if (paper.EndTime.HasValue && DateTime.UtcNow > paper.EndTime.Value)
            throw new InvalidOperationException("考试时间已结束，无法提交答案。");

        var validQuestionIds = paper.ExamQuestions.Select(eq => eq.QuestionId).ToHashSet();
        var submittedAt = DateTime.UtcNow;

        foreach (var answer in command.Answers)
        {
            if (!validQuestionIds.Contains(answer.QuestionId))
                continue; // 忽略不属于该试卷的题目

            // 幂等处理：已存在则更新
            var existing = await context.StudentAnswers
                .FirstOrDefaultAsync(
                    sa => sa.ExamPaperId == command.ExamPaperId
                       && sa.StudentId   == command.StudentId
                       && sa.QuestionId  == answer.QuestionId,
                    cancellationToken);

            if (existing is not null)
            {
                existing.AnswerContent = answer.Content;
                existing.SubmittedAt   = submittedAt;
                existing.GradingStatus = GradingStatus.Pending;
            }
            else
            {
                context.StudentAnswers.Add(new StudentAnswer
                {
                    ExamPaperId   = command.ExamPaperId,
                    QuestionId    = answer.QuestionId,
                    StudentId     = command.StudentId,
                    AnswerContent = answer.Content,
                    SubmittedAt   = submittedAt,
                    GradingStatus = GradingStatus.Pending
                });
            }
        }

        // 对客观题（单选/多选/判断）自动评分
        await AutoGradeObjectiveQuestionsAsync(command, paper, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task AutoGradeObjectiveQuestionsAsync(
        SubmitAnswersCommand command,
        ExamPaper paper,
        CancellationToken cancellationToken)
    {
        var objectiveTypes = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse };

        var examQuestionMap = paper.ExamQuestions.ToDictionary(eq => eq.QuestionId, eq => eq.Score);
        var questionIds     = command.Answers.Select(a => a.QuestionId).ToList();

        var questions = await context.Questions
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id) && objectiveTypes.Contains(q.Type))
            .ToDictionaryAsync(q => q.Id, cancellationToken);

        foreach (var answer in command.Answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                continue;

            var studentAnswer = await context.StudentAnswers
                .FirstOrDefaultAsync(
                    sa => sa.ExamPaperId == command.ExamPaperId
                       && sa.StudentId   == command.StudentId
                       && sa.QuestionId  == answer.QuestionId,
                    cancellationToken);

            if (studentAnswer is null) continue;

            var isCorrect = string.Equals(
                answer.Content.Trim(), question.CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);

            studentAnswer.Score         = isCorrect ? (examQuestionMap.GetValueOrDefault(question.Id, 0)) : 0;
            studentAnswer.GradingStatus = GradingStatus.AutoGraded;
        }
    }
}
