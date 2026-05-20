namespace ExamSystem.Domain.Entities;

/// <summary>在线练习会话记录（服务端持久化，跨设备可恢复）</summary>
public class PracticeSession : Common.BaseEntity
{
    public Guid TenantId { get; set; }
    public string StudentId { get; set; } = "";

    // 成绩数据
    public int Count { get; set; }
    public int CorrectCount { get; set; }
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }

    // 设置快照（用于"再做一次"）
    public string? TypeName { get; set; }
    public string? KnowledgePoint { get; set; }
    public int? QuestionType { get; set; }
    public int? Difficulty { get; set; }
    public int SetupCount { get; set; } = 10;
}
