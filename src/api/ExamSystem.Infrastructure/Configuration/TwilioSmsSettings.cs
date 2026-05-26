namespace ExamSystem.Infrastructure.Configuration;

/// <summary>
/// Twilio 短信发送配置。
/// 推荐通过环境变量注入敏感值：TWILIO_SMS__ACCOUNT_SID、TWILIO_SMS__AUTH_TOKEN。
/// </summary>
public class TwilioSmsSettings
{
    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string? FromPhoneNumber { get; set; }
    public string? MessagingServiceSid { get; set; }
    public string AppName { get; set; } = "ExamSystem";
}