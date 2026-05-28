using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// AI 模型配置，支持按租户 + 场景细粒度配置不同的 AI 提供商和模型。
/// TenantId 为 null 表示系统级默认配置，可被租户级配置覆盖。
/// </summary>
public class AiModelConfig : BaseEntity
{
    /// <summary>所属租户，null 表示系统级配置（超级管理员管理）</summary>
    public Guid? TenantId { get; set; }

    /// <summary>适用的业务场景，Default 表示通用兜底配置</summary>
    public AiScene Scene { get; set; } = AiScene.Default;

    /// <summary>提供商显示名称，如 DeepSeek、OpenAI、Qwen 等</summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>OpenAI 兼容 API 的 Base URL，如 https://api.deepseek.com/v1</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API 密钥（生产环境建议加密存储）</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>对话使用的模型名称</summary>
    public string ChatModel { get; set; } = string.Empty;

    /// <summary>向量嵌入使用的模型名称（可选）</summary>
    public string? EmbeddingModel { get; set; }

    /// <summary>单次请求最大输出 Token 数</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>生成温度（0.0 ~ 2.0）</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>每月 Token 用量配额，null 表示不限制</summary>
    public long? MonthlyQuotaTokens { get; set; }

    /// <summary>本月已使用 Token 数</summary>
    public long UsedTokensCurrentMonth { get; set; } = 0;

    /// <summary>配额重置时间（通常为每月1日），null 表示从未重置</summary>
    public DateTime? QuotaResetAt { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>同场景下多配置的优先级，数值越大越优先</summary>
    public int Priority { get; set; } = 0;

    /// <summary>备注说明</summary>
    public string? Description { get; set; }

    // 导航属性
    public Tenant? Tenant { get; set; }
}

