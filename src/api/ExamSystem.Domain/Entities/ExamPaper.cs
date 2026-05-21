using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 试卷实体，包含组卷策略及关联题目
/// </summary>
public class ExamPaper : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalScore { get; set; }
    public int DurationMinutes { get; set; }

    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    /// <summary>是否开启防作弊监控</summary>
    public bool AntiCheatingEnabled { get; set; } = false;

    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}

/// <summary>
/// 试卷与题目的多对多关联，包含分值和排序
/// </summary>
public class ExamQuestion
{
    public Guid ExamPaperId { get; set; }
    public ExamPaper ExamPaper { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int Score { get; set; }
    public int Order { get; set; }
}
