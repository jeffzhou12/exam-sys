namespace ExamSystem.Application.Common.Interfaces;

public interface ISlidingCaptchaService
{
    /// <summary>生成一道滑动拼图题目，返回背景图、拼图块图及元数据；挑战 ID 存入 Redis。</summary>
    Task<SlidingCaptchaChallenge> GenerateChallengeAsync(CancellationToken ct = default);

    /// <summary>
    /// 验证用户提交的拼图位置。
    /// 若通过则在 Redis 写入单次使用的 token 并返回；否则抛 InvalidOperationException。
    /// </summary>
    Task<string> VerifyAndIssueTokenAsync(string challengeId, double x, CancellationToken ct = default);
}

/// <param name="Id">挑战 ID（用于提交答案）。</param>
/// <param name="BgImage">背景图，data URI 格式（JPEG base64）。</param>
/// <param name="PieceImage">拼图块，data URI 格式（PNG base64）。</param>
/// <param name="BgWidth">背景图宽度（像素）。</param>
/// <param name="BgHeight">背景图高度（像素）。</param>
/// <param name="PieceY">拼图块在背景中的纵向偏移（像素）。</param>
/// <param name="PieceSize">拼图块的边长（像素，正方形）。</param>
public record SlidingCaptchaChallenge(
    string Id,
    string BgImage,
    string PieceImage,
    int BgWidth,
    int BgHeight,
    int PieceY,
    int PieceSize);
