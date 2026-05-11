using System.Text.Json;
using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 题目实体，Options 字段使用 JSONB 存储以支持灵活扩展
/// </summary>
public class Question : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public QuestionType Type { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>选项列表，以 JSON 存储，适配不同题型</summary>
    public JsonDocument? Options { get; set; }

    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? KnowledgePoint { get; set; }

    /// <summary>难度系数 1-5</summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>向量嵌入，用于题目去重检测（pgvector）</summary>
    public float[]? Embedding { get; set; }

    public bool IsAiGenerated { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
