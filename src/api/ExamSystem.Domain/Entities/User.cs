using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

public class User : BaseEntity
{
    /// <summary>所属租户，NULL 表示系统级管理员</summary>
    public Guid? TenantId { get; set; }

    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WeChatOpenId { get; set; }
    public string? WeChatUnionId { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public Tenant? Tenant { get; set; }
}
