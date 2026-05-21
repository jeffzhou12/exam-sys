using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// AI 调用审计日志，用于统计 Token 消耗和追踪 AI 行为
/// </summary>
public class AiAuditLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Operation { get; set; } = string.Empty; // e.g. "GenerateQuestion", "GradeAnswer"
    public string ModelName { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
