using ExamSystem.Domain.Common;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 用户收藏记录（题目 / 试卷 / 图书）
/// </summary>
public class UserFavorite : BaseEntity
{
    public Guid TenantId { get; set; }

    /// <summary>收藏者用户 ID（与 JWT sub 一致，string 类型）</summary>
    public string UserId { get; set; } = string.Empty;

    public FavoriteTargetType TargetType { get; set; }

    /// <summary>被收藏对象的 ID（QuestionId / ExamPaperId / BookId）</summary>
    public Guid TargetId { get; set; }
}
