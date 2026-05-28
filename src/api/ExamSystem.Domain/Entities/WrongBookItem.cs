using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>错题本条目 — 记录学生在练习中答错的题目及次数</summary>
public class WrongBookItem : BaseEntity
{
    public Guid TenantId { get; set; }

    /// <summary>学生 ID（对应 User.Id 的字符串形式）</summary>
    public string StudentId { get; set; } = null!;

    public Guid QuestionId { get; set; }

    /// <summary>学生当时的作答内容（最近一次）</summary>
    public string AnswerGiven { get; set; } = string.Empty;

    /// <summary>累计答错次数</summary>
    public int WrongCount { get; set; } = 1;

    // Navigation
    public Question Question { get; set; } = null!;
}
