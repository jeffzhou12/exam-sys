namespace ExamSystem.Application.Common.Interfaces;

public interface ITenantService
{
    /// <summary>从当前 HTTP 上下文中解析租户 ID</summary>
    Guid GetCurrentTenantId();

    /// <summary>获取当前租户的数据库 Schema 名称</summary>
    string GetCurrentSchemaName();

    /// <summary>验证租户是否存在且处于激活状态</summary>
    Task<bool> ValidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
