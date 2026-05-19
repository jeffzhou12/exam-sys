using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Application.AiConfigs;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>AI 模型配置响应 DTO（ApiKey 脱敏处理）</summary>
public record AiModelConfigDto(
    Guid Id,
    Guid? TenantId,
    AiScene Scene,
    string SceneName,
    string ProviderName,
    string BaseUrl,
    string ApiKeyMasked,       // 仅返回最后4位，如 ****wxyz
    string ChatModel,
    string? EmbeddingModel,
    int MaxTokens,
    double Temperature,
    long? MonthlyQuotaTokens,
    long UsedTokensCurrentMonth,
    DateTime? QuotaResetAt,
    bool IsEnabled,
    int Priority,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ── 查询 ──────────────────────────────────────────────────────────────────────

public record GetAiModelConfigsQuery(Guid? TenantId);

public class GetAiModelConfigsQueryHandler(IAiModelConfigService configService)
{
    public async Task<List<AiModelConfigDto>> Handle(
        GetAiModelConfigsQuery query, CancellationToken ct = default)
    {
        var configs = await configService.GetConfigsAsync(query.TenantId, ct);
        return configs.Select(ToDto).ToList();
    }

    internal static AiModelConfigDto ToDto(AiModelConfig c) => new(
        c.Id, c.TenantId, c.Scene, c.Scene.ToString(),
        c.ProviderName, c.BaseUrl, MaskApiKey(c.ApiKey),
        c.ChatModel, c.EmbeddingModel,
        c.MaxTokens, c.Temperature,
        c.MonthlyQuotaTokens, c.UsedTokensCurrentMonth, c.QuotaResetAt,
        c.IsEnabled, c.Priority, c.Description,
        c.CreatedAt, c.UpdatedAt);

    private static string MaskApiKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Length <= 4
            ? new string('*', key.Length)
            : "****" + key[^4..];
    }
}

public record GetAiModelConfigByIdQuery(Guid Id);

public class GetAiModelConfigByIdQueryHandler(IAiModelConfigService configService)
{
    public async Task<AiModelConfigDto?> Handle(
        GetAiModelConfigByIdQuery query, CancellationToken ct = default)
    {
        var config = await configService.GetByIdAsync(query.Id, ct);
        return config is null ? null : GetAiModelConfigsQueryHandler.ToDto(config);
    }
}

// ── 创建命令 ──────────────────────────────────────────────────────────────────

public record CreateAiModelConfigCommand(
    Guid? TenantId,
    AiScene Scene,
    string ProviderName,
    string BaseUrl,
    string ApiKey,
    string ChatModel,
    string? EmbeddingModel,
    int MaxTokens,
    double Temperature,
    long? MonthlyQuotaTokens,
    bool IsEnabled,
    int Priority,
    string? Description);

public class CreateAiModelConfigCommandHandler(IAiModelConfigService configService)
{
    public async Task<Guid> Handle(CreateAiModelConfigCommand cmd, CancellationToken ct = default)
    {
        var config = new AiModelConfig
        {
            TenantId           = cmd.TenantId,
            Scene              = cmd.Scene,
            ProviderName       = cmd.ProviderName,
            BaseUrl            = cmd.BaseUrl,
            ApiKey             = cmd.ApiKey,
            ChatModel          = cmd.ChatModel,
            EmbeddingModel     = cmd.EmbeddingModel,
            MaxTokens          = cmd.MaxTokens,
            Temperature        = cmd.Temperature,
            MonthlyQuotaTokens = cmd.MonthlyQuotaTokens,
            IsEnabled          = cmd.IsEnabled,
            Priority           = cmd.Priority,
            Description        = cmd.Description,
        };

        var created = await configService.CreateAsync(config, ct);
        return created.Id;
    }
}

// ── 更新命令 ──────────────────────────────────────────────────────────────────

public record UpdateAiModelConfigCommand(
    Guid Id,
    AiScene Scene,
    string ProviderName,
    string BaseUrl,
    string? ApiKey,            // null 表示不修改 ApiKey
    string ChatModel,
    string? EmbeddingModel,
    int MaxTokens,
    double Temperature,
    long? MonthlyQuotaTokens,
    bool IsEnabled,
    int Priority,
    string? Description);

public class UpdateAiModelConfigCommandHandler(IAiModelConfigService configService)
{
    public async Task Handle(UpdateAiModelConfigCommand cmd, CancellationToken ct = default)
    {
        var existing = await configService.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"AI 配置 {cmd.Id} 不存在");

        existing.Scene              = cmd.Scene;
        existing.ProviderName       = cmd.ProviderName;
        existing.BaseUrl            = cmd.BaseUrl;
        existing.ChatModel          = cmd.ChatModel;
        existing.EmbeddingModel     = cmd.EmbeddingModel;
        existing.MaxTokens          = cmd.MaxTokens;
        existing.Temperature        = cmd.Temperature;
        existing.MonthlyQuotaTokens = cmd.MonthlyQuotaTokens;
        existing.IsEnabled          = cmd.IsEnabled;
        existing.Priority           = cmd.Priority;
        existing.Description        = cmd.Description;

        // 仅当传入新 ApiKey 时才更新
        if (!string.IsNullOrWhiteSpace(cmd.ApiKey))
            existing.ApiKey = cmd.ApiKey;

        await configService.UpdateAsync(existing, ct);
    }
}

// ── 删除命令 ──────────────────────────────────────────────────────────────────

public record DeleteAiModelConfigCommand(Guid Id);

public class DeleteAiModelConfigCommandHandler(IAiModelConfigService configService)
{
    public async Task Handle(DeleteAiModelConfigCommand cmd, CancellationToken ct = default)
        => await configService.DeleteAsync(cmd.Id, ct);
}

// ── 重置月度用量命令 ──────────────────────────────────────────────────────────

public record ResetAiModelConfigQuotaCommand(Guid Id);

public class ResetAiModelConfigQuotaCommandHandler(IAiModelConfigService configService)
{
    public async Task Handle(ResetAiModelConfigQuotaCommand cmd, CancellationToken ct = default)
    {
        var config = await configService.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"AI 配置 {cmd.Id} 不存在");

        config.UsedTokensCurrentMonth = 0;
        config.QuotaResetAt           = DateTime.UtcNow;
        await configService.UpdateAsync(config, ct);
    }
}
