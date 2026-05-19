namespace ExamSystem.Infrastructure.Configuration;

/// <summary>
/// 文件存储全局配置。
/// 对应 appsettings.json 中的 "Storage" 节点。
/// </summary>
public class StorageSettings
{
    /// <summary>存储提供商：local（本地磁盘）或 s3（AWS S3）</summary>
    public string Provider { get; set; } = "local";

    public S3Config S3 { get; set; } = new();

    public class S3Config
    {
        public string Region { get; set; } = "ap-northeast-1";
        public int PresignedUrlExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 模块级别 Bucket 映射。Key 为模块名（如 Books、Media、Default），Value 为 Bucket 名称。
        /// 未配置的模块回落到 Default；Default 必须有值才能启动 S3 模式。
        /// </summary>
        public Dictionary<string, string> Buckets { get; set; } = new();

        /// <summary>返回指定模块的 Bucket 名称，找不到则回落 Default。</summary>
        public string ResolveBucket(string module)
        {
            if (Buckets.TryGetValue(module, out var b) && !string.IsNullOrWhiteSpace(b))
                return b;
            if (Buckets.TryGetValue("Default", out var def) && !string.IsNullOrWhiteSpace(def))
                return def;
            throw new InvalidOperationException(
                $"未找到模块 '{module}' 的 S3 Bucket 配置，请在 Storage:S3:Buckets 中配置 '{module}' 或 'Default'。");
        }
    }
}
