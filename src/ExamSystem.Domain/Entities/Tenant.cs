using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 租户实体，代表一个独立的考试组织单位（如学校/部门）
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>AI 调用次数配额（每月）</summary>
    public int AiCallQuota { get; set; } = 1000;
    public int AiCallUsed { get; set; } = 0;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamPaper> ExamPapers { get; set; } = new List<ExamPaper>();
}
