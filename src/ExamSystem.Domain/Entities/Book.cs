using ExamSystem.Domain.Common;

namespace ExamSystem.Domain.Entities;

/// <summary>图书</summary>
public class Book : BaseEntity
{
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>PDF 文件在服务器上的相对存储路径</summary>
    public string? PdfFilePath { get; set; }

    /// <summary>分类</summary>
    public string? Category { get; set; }

    /// <summary>标签列表，JSON 数组字符串，例如 ["计算机","网络"]</summary>
    public string? Tags { get; set; }

    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
}
