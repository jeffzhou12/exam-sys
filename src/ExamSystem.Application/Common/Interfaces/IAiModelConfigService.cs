using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Application.Common.Interfaces;

/// <summary>AI 模型配置服务：提供配置的增删改查及场景路由解析</summary>
public interface IAiModelConfigService
{
    /// <summary>
    /// 按优先级解析最佳配置，回退链：租户+场景 → 租户+默认 → 系统+场景 → 系统+默认
    /// </summary>
    Task<AiModelConfig?> ResolveConfigAsync(Guid? tenantId, AiScene scene, CancellationToken ct = default);

    /// <summary>获取指定租户（或系统级）的所有配置</summary>
    Task<List<AiModelConfig>> GetConfigsAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>获取单个配置详情</summary>
    Task<AiModelConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>创建配置</summary>
    Task<AiModelConfig> CreateAsync(AiModelConfig config, CancellationToken ct = default);

    /// <summary>更新配置</summary>
    Task<AiModelConfig> UpdateAsync(AiModelConfig config, CancellationToken ct = default);

    /// <summary>删除配置</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>增加 Token 用量统计，自动检测并重置月度配额</summary>
    Task IncrementUsageAsync(Guid configId, long tokensUsed, CancellationToken ct = default);

    /// <summary>检查指定配置是否还有可用配额</summary>
    Task<bool> HasAvailableQuotaAsync(Guid configId, CancellationToken ct = default);
}
