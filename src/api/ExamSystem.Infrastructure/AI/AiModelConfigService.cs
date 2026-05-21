using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Infrastructure.AI;

/// <summary>
/// AI 模型配置服务实现。
/// 解析链：租户+场景 → 租户+默认 → 系统+场景 → 系统+默认
/// </summary>
public class AiModelConfigService(
    ApplicationDbContext db,
    ILogger<AiModelConfigService> logger) : IAiModelConfigService
{
    public async Task<AiModelConfig?> ResolveConfigAsync(
        Guid? tenantId, AiScene scene, CancellationToken ct = default)
    {
        // 优先级降序查询：先精确匹配，再兜底 Default
        var candidates = await db.AiModelConfigs
            .Where(c => c.IsEnabled &&
                        (c.TenantId == tenantId || c.TenantId == null) &&
                        (c.Scene == scene || c.Scene == AiScene.Default))
            .OrderByDescending(c => c.TenantId != null)   // 租户级优先于系统级
            .ThenByDescending(c => c.Scene == scene)      // 精确场景优先于 Default
            .ThenByDescending(c => c.Priority)
            .ToListAsync(ct);

        // 按照优先级查找配额未超限的配置
        foreach (var cfg in candidates)
        {
            if (cfg.MonthlyQuotaTokens.HasValue)
            {
                // 检查是否需要重置月度用量
                if (cfg.QuotaResetAt.HasValue && DateTime.UtcNow.Month != cfg.QuotaResetAt.Value.Month)
                {
                    cfg.UsedTokensCurrentMonth = 0;
                    cfg.QuotaResetAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }

                if (cfg.UsedTokensCurrentMonth >= cfg.MonthlyQuotaTokens.Value)
                {
                    logger.LogWarning("AI 配置 {Id} ({Provider}/{Scene}) 已超过月度配额 {Quota}，跳过",
                        cfg.Id, cfg.ProviderName, cfg.Scene, cfg.MonthlyQuotaTokens);
                    continue;
                }
            }

            return cfg;
        }

        logger.LogWarning("租户 {TenantId} 场景 {Scene} 未找到可用的 AI 配置", tenantId, scene);
        return null;
    }

    public async Task<List<AiModelConfig>> GetConfigsAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.AiModelConfigs
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.Scene)
            .ToListAsync(ct);

    public async Task<AiModelConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.AiModelConfigs.FindAsync([id], ct);

    public async Task<AiModelConfig> CreateAsync(AiModelConfig config, CancellationToken ct = default)
    {
        db.AiModelConfigs.Add(config);
        await db.SaveChangesAsync(ct);
        return config;
    }

    public async Task<AiModelConfig> UpdateAsync(AiModelConfig config, CancellationToken ct = default)
    {
        db.AiModelConfigs.Update(config);
        await db.SaveChangesAsync(ct);
        return config;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var config = await db.AiModelConfigs.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"AI 配置 {id} 不存在");
        db.AiModelConfigs.Remove(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task IncrementUsageAsync(Guid configId, long tokensUsed, CancellationToken ct = default)
    {
        await db.AiModelConfigs
            .Where(c => c.Id == configId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.UsedTokensCurrentMonth,
                              c => c.UsedTokensCurrentMonth + tokensUsed),
                ct);
    }

    public async Task<bool> HasAvailableQuotaAsync(Guid configId, CancellationToken ct = default)
    {
        var config = await db.AiModelConfigs
            .AsNoTracking()
            .Where(c => c.Id == configId)
            .Select(c => new { c.MonthlyQuotaTokens, c.UsedTokensCurrentMonth })
            .FirstOrDefaultAsync(ct);

        if (config is null) return false;
        if (!config.MonthlyQuotaTokens.HasValue) return true;
        return config.UsedTokensCurrentMonth < config.MonthlyQuotaTokens.Value;
    }
}
