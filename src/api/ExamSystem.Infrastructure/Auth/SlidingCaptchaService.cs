using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ExamSystem.Infrastructure.Auth;

/// <summary>
/// 基于 SixLabors.ImageSharp 3.x（Apache 2.0）的滑动拼图验证码服务。
/// 仅使用核心包，通过 ProcessPixelRows 直接操作像素，无需 Drawing 扩展包授权。
/// 已知 ImageSharp 3.x 的 DoS 漏洞仅影响解码不受信任的图片，本服务仅生成图片，不受影响。
/// </summary>
public class SlidingCaptchaService(
    ICacheService cache,
    AuthProtectionSettings settings) : ISlidingCaptchaService
{
    private const int BgWidth = 280;
    private const int BgHeight = 155;
    private const int PieceSize = 44;

    private const int PieceMinX = PieceSize + 20;
    private const int PieceMaxX = BgWidth - PieceSize - 20;

    public async Task<SlidingCaptchaChallenge> GenerateChallengeAsync(CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var rng = Random.Shared;

        int expectedX = rng.Next(PieceMinX, PieceMaxX);
        int pieceY = rng.Next(10, BgHeight - PieceSize - 10);

        string bgBase64, pieceBase64;

        using (var bg = GenerateBackground(rng))
        {
            // 1. 从原图裁剪拼图块区域（在挖孔之前）
            var pieceRect = new Rectangle(expectedX, pieceY, PieceSize, PieceSize);
            using var piece = bg.Clone(ctx => ctx.Crop(pieceRect));

            // 给拼图块加白色边框
            DrawBorder(piece, 0, 0, PieceSize, PieceSize, new Rgba32(255, 255, 255, 210), 1);

            // 2. 在背景上挖出暗孔
            FillRect(bg, expectedX, pieceY, PieceSize, PieceSize, new Rgba32(0, 0, 0, 160));
            DrawBorder(bg, expectedX, pieceY, PieceSize, PieceSize, new Rgba32(255, 255, 255, 200), 1);

            using var bgStream = new MemoryStream();
            await bg.SaveAsJpegAsync(bgStream, cancellationToken: ct);
            bgBase64 = Convert.ToBase64String(bgStream.ToArray());

            using var pieceStream = new MemoryStream();
            await piece.SaveAsPngAsync(pieceStream, cancellationToken: ct);
            pieceBase64 = Convert.ToBase64String(pieceStream.ToArray());
        }

        var expiry = TimeSpan.FromMinutes(settings.Captcha.ChallengeExpiryMinutes);
        await cache.SetAsync($"auth:captcha:challenge:{id}", expectedX, expiry, ct);

        return new SlidingCaptchaChallenge(
            id,
            $"data:image/jpeg;base64,{bgBase64}",
            $"data:image/png;base64,{pieceBase64}",
            BgWidth, BgHeight, pieceY, PieceSize);
    }

    public async Task<string> VerifyAndIssueTokenAsync(string challengeId, double x, CancellationToken ct = default)
    {
        var key = $"auth:captcha:challenge:{challengeId}";
        var expectedX = await cache.GetAsync<int?>(key, ct);

        if (expectedX is null)
            throw new InvalidOperationException("验证码已过期，请重新获取。");

        // 一次性：无论通过与否都删除，防止暴力枚举
        await cache.RemoveAsync(key, ct);

        if (Math.Abs(x - expectedX.Value) > settings.Captcha.Tolerance)
            throw new InvalidOperationException("拼图位置不正确，请重试。");

        var token = Guid.NewGuid().ToString("N");
        var tokenExpiry = TimeSpan.FromMinutes(settings.Captcha.VerifiedTokenExpiryMinutes);
        await cache.SetAsync($"auth:captcha:verified:{token}", "1", tokenExpiry, ct);

        return token;
    }

    // ── 图像生成（纯 ProcessPixelRows）──────────────────────────────────────

    private static Image<Rgba32> GenerateBackground(Random rng)
    {
        var img = new Image<Rgba32>(BgWidth, BgHeight);

        // 底色（偏蓝绿系）
        byte baseR = (byte)rng.Next(60, 130);
        byte baseG = (byte)rng.Next(80, 155);
        byte baseB = (byte)rng.Next(120, 200);
        FillRect(img, 0, 0, BgWidth, BgHeight, new Rgba32(baseR, baseG, baseB, 255));

        // 12 个随机半透明色块
        for (int i = 0; i < 12; i++)
        {
            FillRect(img,
                rng.Next(0, BgWidth - 20),
                rng.Next(0, BgHeight - 8),
                rng.Next(20, 95),
                rng.Next(12, 55),
                new Rgba32(
                    (byte)rng.Next(20, 230),
                    (byte)rng.Next(20, 230),
                    (byte)rng.Next(20, 230),
                    (byte)rng.Next(60, 155)));
        }

        // 5 条随机对角线
        for (int i = 0; i < 5; i++)
        {
            DrawLine(img,
                rng.Next(0, BgWidth), rng.Next(0, BgHeight),
                rng.Next(0, BgWidth), rng.Next(0, BgHeight),
                new Rgba32(255, 255, 255, (byte)rng.Next(40, 110)));
        }

        return img;
    }

    /// <summary>Alpha 混合填充矩形区域。</summary>
    private static void FillRect(Image<Rgba32> img, int rx, int ry, int rw, int rh, Rgba32 color)
    {
        int x1 = Math.Clamp(rx, 0, img.Width);
        int y1 = Math.Clamp(ry, 0, img.Height);
        int x2 = Math.Clamp(rx + rw, 0, img.Width);
        int y2 = Math.Clamp(ry + rh, 0, img.Height);
        if (x1 >= x2 || y1 >= y2) return;

        float alpha = color.A / 255f;
        img.ProcessPixelRows(accessor =>
        {
            for (int y = y1; y < y2; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = x1; x < x2; x++)
                {
                    ref var px = ref row[x];
                    px = new Rgba32(
                        (byte)(px.R * (1 - alpha) + color.R * alpha),
                        (byte)(px.G * (1 - alpha) + color.G * alpha),
                        (byte)(px.B * (1 - alpha) + color.B * alpha),
                        255);
                }
            }
        });
    }

    /// <summary>绘制矩形边框（不填充内部）。</summary>
    private static void DrawBorder(Image<Rgba32> img, int rx, int ry, int rw, int rh, Rgba32 color, int thickness)
    {
        FillRect(img, rx, ry, rw, thickness, color);
        FillRect(img, rx, ry + rh - thickness, rw, thickness, color);
        FillRect(img, rx, ry, thickness, rh, color);
        FillRect(img, rx + rw - thickness, ry, thickness, rh, color);
    }

    /// <summary>Bresenham 直线算法绘制单像素线段。</summary>
    private static void DrawLine(Image<Rgba32> img, int x0, int y0, int x1, int y1, Rgba32 color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        float alpha = color.A / 255f;

        img.ProcessPixelRows(accessor =>
        {
            int cx = x0, cy = y0;
            while (true)
            {
                if (cx >= 0 && cx < img.Width && cy >= 0 && cy < img.Height)
                {
                    var row = accessor.GetRowSpan(cy);
                    ref var px = ref row[cx];
                    px = new Rgba32(
                        (byte)(px.R * (1 - alpha) + color.R * alpha),
                        (byte)(px.G * (1 - alpha) + color.G * alpha),
                        (byte)(px.B * (1 - alpha) + color.B * alpha),
                        255);
                }

                if (cx == x1 && cy == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { if (cx == x1) break; err += dy; cx += sx; }
                if (e2 <= dx) { if (cy == y1) break; err += dx; cy += sy; }
            }
        });
    }
}
