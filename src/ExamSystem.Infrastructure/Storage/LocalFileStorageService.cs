using ExamSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ExamSystem.Infrastructure.Storage;

/// <summary>本地磁盘存储服务，将上传文件保存到 uploads 目录（仅用于本地开发）</summary>
public class LocalFileStorageService(IHostEnvironment env) : IFileStorageService
{
    private readonly string _rootPath = Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(Stream stream, string fileName, string subfolder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_rootPath, subfolder);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var newFileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, newFileName);

        await using var dest = File.Create(fullPath);
        await stream.CopyToAsync(dest, ct);

        return $"{subfolder}/{newFileName}".Replace('\\', '/');
    }

    public Task<Stream> GetStreamAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    /// <summary>本地存储不支持预签名 URL，返回 null（调用方会改为流式输出）</summary>
    public Task<string?> GetPresignedUrlAsync(string key, int expirationMinutes = 60) =>
        Task.FromResult<string?>(null);

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
