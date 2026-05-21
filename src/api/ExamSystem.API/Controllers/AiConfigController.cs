using ExamSystem.Application.AiConfigs;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

/// <summary>
/// AI 模型配置管理接口。
/// - 超级管理员：可管理系统级配置（TenantId=null）及任意租户的配置
/// - 租户管理员（Admin）：只能管理自己租户下的配置
/// </summary>
[Authorize(Roles = Roles.SuperAdminOrAdmin)]
[ApiController]
[Route("api/ai-configs")]
[Produces("application/json")]
public class AiConfigController(
    GetAiModelConfigsQueryHandler getConfigsHandler,
    GetAiModelConfigByIdQueryHandler getByIdHandler,
    CreateAiModelConfigCommandHandler createHandler,
    UpdateAiModelConfigCommandHandler updateHandler,
    DeleteAiModelConfigCommandHandler deleteHandler,
    ResetAiModelConfigQuotaCommandHandler resetQuotaHandler,
    ITenantService tenantService) : ControllerBase
{
    /// <summary>获取 AI 模型配置列表</summary>
    /// <remarks>
    /// 超级管理员可通过 tenantId 查询指定租户配置，或不传 tenantId 查询系统级配置。
    /// 租户管理员只能查询自己租户的配置。
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<AiModelConfigDto>), 200)]
    public async Task<IActionResult> GetConfigs(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        var result = await getConfigsHandler.Handle(
            new GetAiModelConfigsQuery(resolvedTenantId), cancellationToken);
        return Ok(result);
    }

    /// <summary>获取单个 AI 模型配置详情</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AiModelConfigDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await getByIdHandler.Handle(new GetAiModelConfigByIdQuery(id), cancellationToken);
        if (result is null) return NotFound();

        // 租户管理员不允许查看其他租户的配置
        if (!IsSuperAdmin() && result.TenantId != tenantService.GetCurrentTenantId())
            return Forbid();

        return Ok(result);
    }

    /// <summary>创建 AI 模型配置</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAiModelConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = ResolveTenantId(request.TenantId);

        var id = await createHandler.Handle(new CreateAiModelConfigCommand(
            TenantId:           resolvedTenantId,
            Scene:              request.Scene,
            ProviderName:       request.ProviderName,
            BaseUrl:            request.BaseUrl,
            ApiKey:             request.ApiKey,
            ChatModel:          request.ChatModel,
            EmbeddingModel:     request.EmbeddingModel,
            MaxTokens:          request.MaxTokens,
            Temperature:        request.Temperature,
            MonthlyQuotaTokens: request.MonthlyQuotaTokens,
            IsEnabled:          request.IsEnabled,
            Priority:           request.Priority,
            Description:        request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>更新 AI 模型配置</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAiModelConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureOwnershipAsync(id, cancellationToken);

        await updateHandler.Handle(new UpdateAiModelConfigCommand(
            Id:                 id,
            Scene:              request.Scene,
            ProviderName:       request.ProviderName,
            BaseUrl:            request.BaseUrl,
            ApiKey:             request.ApiKey,
            ChatModel:          request.ChatModel,
            EmbeddingModel:     request.EmbeddingModel,
            MaxTokens:          request.MaxTokens,
            Temperature:        request.Temperature,
            MonthlyQuotaTokens: request.MonthlyQuotaTokens,
            IsEnabled:          request.IsEnabled,
            Priority:           request.Priority,
            Description:        request.Description),
            cancellationToken);

        return NoContent();
    }

    /// <summary>删除 AI 模型配置</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureOwnershipAsync(id, cancellationToken);
        await deleteHandler.Handle(new DeleteAiModelConfigCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>重置指定配置的月度 Token 用量（手动重置）</summary>
    [HttpPost("{id:guid}/reset-quota")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetQuota(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureOwnershipAsync(id, cancellationToken);
        await resetQuotaHandler.Handle(new ResetAiModelConfigQuotaCommand(id), cancellationToken);
        return NoContent();
    }

    // ── 内部辅助 ─────────────────────────────────────────────────────────────

    private bool IsSuperAdmin() =>
        User.IsInRole(Roles.SuperAdmin);

    /// <summary>
    /// 解析目标 TenantId：
    /// - 超级管理员：使用请求中指定的 tenantId（可为 null 表示系统级）
    /// - 租户管理员：强制使用自己的 TenantId
    /// </summary>
    private Guid? ResolveTenantId(Guid? requestedTenantId) =>
        IsSuperAdmin() ? requestedTenantId : tenantService.GetCurrentTenantId();

    /// <summary>验证当前用户是否有权操作指定配置（租户管理员只能操作自己租户的配置）</summary>
    private async Task EnsureOwnershipAsync(Guid configId, CancellationToken ct)
    {
        if (IsSuperAdmin()) return;

        var config = await getByIdHandler.Handle(new GetAiModelConfigByIdQuery(configId), ct);
        if (config is null) throw new KeyNotFoundException($"AI 配置 {configId} 不存在");

        if (config.TenantId != tenantService.GetCurrentTenantId())
            throw new UnauthorizedAccessException("无权操作其他租户的 AI 配置");
    }
}

// ── 请求 DTO ──────────────────────────────────────────────────────────────────

public record CreateAiModelConfigRequest(
    Guid? TenantId,
    AiScene Scene,
    string ProviderName,
    string BaseUrl,
    string ApiKey,
    string ChatModel,
    string? EmbeddingModel,
    int MaxTokens = 4096,
    double Temperature = 0.7,
    long? MonthlyQuotaTokens = null,
    bool IsEnabled = true,
    int Priority = 0,
    string? Description = null);

public record UpdateAiModelConfigRequest(
    AiScene Scene,
    string ProviderName,
    string BaseUrl,
    string? ApiKey,       // null 表示不修改 ApiKey
    string ChatModel,
    string? EmbeddingModel,
    int MaxTokens = 4096,
    double Temperature = 0.7,
    long? MonthlyQuotaTokens = null,
    bool IsEnabled = true,
    int Priority = 0,
    string? Description = null);
