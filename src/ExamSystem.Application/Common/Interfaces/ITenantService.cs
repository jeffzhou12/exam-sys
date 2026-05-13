namespace ExamSystem.Application.Common.Interfaces;

public interface ITenantService
{
    /// <summary>从当前 HTTP 上下文中解析租户 ID（SuperAdmin 无租户时返回 null，其余角色无头时抛异常）</summary>
    Guid? GetCurrentTenantId();

    /// <summary>尝试解析租户 ID，不抛异常：无头或无效时返回 null（用于允许匿名/跨租户浏览的接口）</summary>
    Guid? TryGetCurrentTenantId();

    /// <summary>获取当前租户的数据库 Schema 名称</summary>
    string GetCurrentSchemaName();

    /// <summary>验证租户是否存在且处于激活状态</summary>
    Task<bool> ValidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
