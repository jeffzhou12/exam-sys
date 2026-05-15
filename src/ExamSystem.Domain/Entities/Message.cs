using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>站内信</summary>
public class Message : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;

    public Guid RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>附带的题目 ID 列表（JSON 数组字符串）</summary>
    public string? AttachedQuestionIds { get; set; }

    public Guid? AttachedExamPaperId { get; set; }

    public bool IsRead { get; set; } = false;
}
