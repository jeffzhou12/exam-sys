using Amazon.S3;
using Amazon.S3.Model;
using ExamSystem.Application.Common.Interfaces;

namespace ExamSystem.Infrastructure.Storage;

/// <summary>
/// AWS S3 文件存储服务。
/// 生产环境（ECS）凭证由 IAM 任务角色自动提供；
/// 本地开发可通过 AWS 环境变量或 ~/.aws/credentials 提供凭证。
/// 每个实例绑定一个具体的 Bucket，由 FileStorageFactory 按模块创建。
/// </summary>
public class S3FileStorageService(IAmazonS3 s3Client, string bucketName, int presignedUrlExpirationMinutes = 60) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream stream, string fileName, string subfolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var key = $"{subfolder}/{Guid.NewGuid()}{ext}";
        await InitBucketAsync(ct);
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
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
        try
        {
            var response = await s3Client.GetObjectAsync(bucketName, key, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            throw new FileNotFoundException($"文件在存储中不存在：{key}", ex);
        }
    }

    /// <summary>生成预签名 URL，客户端可直接从 S3 下载，节省服务器带宽。</summary>
    public Task<string?> GetPresignedUrlAsync(string key, int expirationMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes > 0 ? expirationMinutes : presignedUrlExpirationMinutes),
            Verb = HttpVerb.GET,
        };

        var url = s3Client.GetPreSignedURL(request);
        return Task.FromResult<string?>(url);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        await s3Client.DeleteObjectAsync(bucketName, key, ct);
    }

    private async Task InitBucketAsync(CancellationToken ct = default)
    {
        if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3Client,bucketName))
        {
            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
            }, ct);
        }
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
