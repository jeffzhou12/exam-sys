namespace ExamSystem.Application.Common.Interfaces;

/// <summary>文件存储服务（本地磁盘或 AWS S3 均可实现此接口）</summary>
public interface IFileStorageService
{
    /// <summary>上传文件，返回存储键（S3 Key 或本地相对路径）</summary>
    Task<string> SaveAsync(Stream stream, string fileName, string subfolder, CancellationToken cancellationToken = default);

    /// <summary>获取文件内容流（本地：直接读取；S3：从对象存储下载）</summary>
    Task<Stream> GetStreamAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>生成预签名临时访问 URL（S3 场景）；本地存储返回 null</summary>
    Task<string?> GetPresignedUrlAsync(string key, int expirationMinutes = 60);

    /// <summary>删除文件</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
