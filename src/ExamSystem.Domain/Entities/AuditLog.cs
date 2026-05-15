namespace ExamSystem.Domain.Entities;

/// <summary>
/// 全局审计日志，记录所有 API 请求的操作轨迹（不可变，无 UpdatedAt）
/// </summary>
public class AuditLog
{
    public Guid    Id          { get; set; } = Guid.NewGuid();
    public Guid?   TenantId    { get; set; }
    public Guid?   UserId      { get; set; }
    public string? Username    { get; set; }
    public string? Role        { get; set; }

    /// <summary>HTTP 方法：GET POST PUT PATCH DELETE</summary>
    public string  Action      { get; set; } = string.Empty;

    /// <summary>受影响的资源类型，如 Question / ExamPaper / User</summary>
    public string? EntityType  { get; set; }

    /// <summary>受影响的资源 ID（字符串，兼容 UUID 和其他格式）</summary>
    public string? EntityId    { get; set; }

    public string  RequestPath { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int     StatusCode  { get; set; }
    public int     DurationMs  { get; set; }
    public string? IpAddress   { get; set; }
    public string? UserAgent   { get; set; }

    /// <summary>变更前的字段快照（UPDATE/DELETE 时由业务层填充）</summary>
    public string? OldValues   { get; set; }

    /// <summary>变更后的字段快照（CREATE/UPDATE 时由业务层填充）</summary>
    public string? NewValues   { get; set; }

    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
