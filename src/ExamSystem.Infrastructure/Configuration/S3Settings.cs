namespace ExamSystem.Infrastructure.Configuration;

public class S3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-northeast-1";
    public int PresignedUrlExpirationMinutes { get; set; } = 60;
}
