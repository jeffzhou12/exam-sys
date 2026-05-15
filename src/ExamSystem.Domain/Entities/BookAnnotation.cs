using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>
/// 图书标注（书签 / 阅读备注 / AI 问答），每条记录对应用户在某本书某一页的一次标注操作。
/// </summary>
public class BookAnnotation : BaseEntity
{
    public Guid BookId { get; set; }
    public Book? Book { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public int PageNumber { get; set; }

    /// <summary>用户框选的原文片段</summary>
    public string? SelectedText { get; set; }

    /// <summary>用户手写备注内容</summary>
    public string? Note { get; set; }

    /// <summary>1=书签  2=阅读备注  3=AI问答</summary>
    public int AnnotationType { get; set; } = 1;

    /// <summary>AI 提问内容（AnnotationType=3 时使用）</summary>
    public string? AiQuestion { get; set; }

    /// <summary>AI 回答内容</summary>
    public string? AiAnswer { get; set; }

    /// <summary>
    /// 标注在页面上的位置信息（JSON），格式：
    /// { "x": 0.12, "y": 0.35, "width": 0.6, "height": 0.08 }
    /// 值为相对于页面宽/高的比例（0~1）
    /// </summary>
    public string? PositionJson { get; set; }

    public string HighlightColor { get; set; } = "#FFEB3B";
}
