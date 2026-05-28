using ExamSystem.Domain.Common;
namespace ExamSystem.Domain.Entities;


/// <summary>
/// 短信模板配置，支持系统级与租户级覆盖。
/// TemplateBody 支持占位符：{code}、{scene}、{target}、{appName}。
/// </summary>
public class SmsTemplate : BaseEntity
{
    /// <summary>所属租户，null 表示系统级模板。</summary>
    public Guid? TenantId { get; set; }

    /// <summary>业务场景标识，如 login、register、reset-password。</summary>
    public string Scene { get; set; } = string.Empty;

    /// <summary>模板名称，便于后台识别。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>短信正文模板。</summary>
    public string TemplateBody { get; set; } = string.Empty;

    /// <summary>是否启用。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>同场景优先级，数值越大越优先。</summary>
    public int Priority { get; set; } = 0;

    /// <summary>备注说明。</summary>
    public string? Description { get; set; }

    public Tenant? Tenant { get; set; }
}