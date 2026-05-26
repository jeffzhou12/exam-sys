namespace ExamSystem.Infrastructure.Configuration;

public class AuthProtectionSettings
{
    public CaptchaConfig Captcha { get; set; } = new();
    public RateLimitPolicy Login { get; set; } = new() { CooldownSeconds = 3, MaxRequests = 12, WindowMinutes = 10 };
    public RateLimitPolicy CodeLogin { get; set; } = new() { CooldownSeconds = 3, MaxRequests = 12, WindowMinutes = 10 };
    public RateLimitPolicy Register { get; set; } = new() { CooldownSeconds = 10, MaxRequests = 6, WindowMinutes = 30 };
    public RateLimitPolicy SendCode { get; set; } = new() { CooldownSeconds = 10, MaxRequests = 8, WindowMinutes = 10 };

    public class CaptchaConfig
    {
        public bool Enabled { get; set; }
        public bool BypassInDevelopment { get; set; } = true;
        /// <summary>像素容差：用户拖动位置与期望位置相差不超过该值视为通过。</summary>
        public double Tolerance { get; set; } = 6.0;
        /// <summary>验证题目在 Redis 中的有效时长（分钟）。</summary>
        public int ChallengeExpiryMinutes { get; set; } = 3;
        /// <summary>通过后颁发的单次 token 在 Redis 中的有效时长（分钟）。</summary>
        public int VerifiedTokenExpiryMinutes { get; set; } = 3;
    }

    public class RateLimitPolicy
    {
        public int CooldownSeconds { get; set; }
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}