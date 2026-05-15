using Amazon.S3;
using Amazon.S3.Model;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.Configuration;

namespace ExamSystem.Infrastructure.Storage;

/// <summary>
/// AWS S3 文件存储服务。
/// 生产环境（ECS）凭证由 IAM 任务角色自动提供；
/// 本地开发可通过 AWS 环境变量或 ~/.aws/credentials 提供凭证。
/// </summary>
public class S3FileStorageService(IAmazonS3 s3Client, S3Settings settings) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream stream, string fileName, string subfolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var key = $"{subfolder}/{Guid.NewGuid()}{ext}";

        var request = new PutObjectRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = GetContentType(ext),
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        };

        await s3Client.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> GetStreamAsync(string key, CancellationToken ct = default)
    {
        var response = await s3Client.GetObjectAsync(settings.BucketName, key, ct);
        return response.ResponseStream;
    }

    /// <summary>生成预签名 URL，客户端可直接从 S3 下载，节省服务器带宽。</summary>
    public Task<string?> GetPresignedUrlAsync(string key, int expirationMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes > 0 ? expirationMinutes : settings.PresignedUrlExpirationMinutes),
            Verb = HttpVerb.GET,
        };

        var url = s3Client.GetPreSignedURL(request);
        return Task.FromResult<string?>(url);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        await s3Client.DeleteObjectAsync(settings.BucketName, key, ct);
    }

    private static string GetContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf"          => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"          => "image/png",
        ".gif"          => "image/gif",
        ".webp"         => "image/webp",
        _               => "application/octet-stream",
    };
}
