using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 考生答题记录
/// </summary>
public class StudentAnswer : BaseEntity
{
    public Guid ExamPaperId { get; set; }
    public ExamPaper ExamPaper { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>考生用户 ID（来自身份系统）</summary>
    public string StudentId { get; set; } = string.Empty;

    public string AnswerContent { get; set; } = string.Empty;

    public int? Score { get; set; }
    public GradingStatus GradingStatus { get; set; } = GradingStatus.Pending;

    /// <summary>AI 评分给出的评语</summary>
    public string? AiFeedback { get; set; }

    public DateTime? SubmittedAt { get; set; }
}
