using ExamSystem.Application.Auth.Commands;
using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Tenants.Queries;
using ExamSystem.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController(
    LoginCommandHandler loginHandler,
    RegisterCommandHandler registerHandler,
    ForgotPasswordCommandHandler forgotPasswordHandler,
    ResetPasswordCommandHandler resetPasswordHandler,
    GetTenantsQueryHandler getTenantsHandler,
    IAuthProtectionService authProtectionService,
    ISlidingCaptchaService slidingCaptchaService,
    IVerificationCodeService codeService,
    VerifyCodeLoginCommandHandler verifyCodeLoginHandler) : ControllerBase
{
    /// <summary>用户登录，获取 JWT 访问令牌（支持用户名/邮箱/手机号/微信 OpenID）</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await authProtectionService.AssertLoginAllowedAsync(request.Identifier, request.CaptchaToken, cancellationToken);
        var result = await loginHandler.HandleAsync(new LoginCommand(request.Identifier, request.Password), cancellationToken);

        if (result is null)
            return Unauthorized(new { error = "账号或密码错误。" });

        return Ok(result);
    }

    /// <summary>发送手机/邮箱验证码（有效期 5 分钟，1 分钟内不可重复发送）</summary>
    [HttpPost("send-code")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SendCode([FromBody] SendCodeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Target))
            return BadRequest(new { error = "请填写手机号或邮箱。" });

        await authProtectionService.AssertSendCodeAllowedAsync(request.Target.Trim(), request.CaptchaToken, cancellationToken);

        var scene = string.IsNullOrWhiteSpace(request.Purpose) ? "login" : request.Purpose.Trim().ToLower();
        var devCode = await codeService.SendCodeAsync(request.Target.Trim(), scene, cancellationToken);
        var msg = request.Target.Contains('@') ? "验证码已发送至邮箱" : "验证码已发送至手机";
        return Ok(new { message = msg, devCode });
    }

    /// <summary>验证码登录（手机/邮箱 + 6 位验证码，首次自动注册）</summary>
    [HttpPost("login-code")]
    [ProducesResponseType(typeof(LoginResult), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> LoginWithCode([FromBody] CodeLoginRequest request, CancellationToken cancellationToken)
    {
        await authProtectionService.AssertCodeLoginAllowedAsync(request.Target, request.CaptchaToken, cancellationToken);

        var (result, error) = await verifyCodeLoginHandler.HandleAsync(
            new VerifyCodeLoginCommand(request.Target, request.Code, request.TenantId, request.Role, request.Nickname),
            cancellationToken);

        if (result is null)
            return BadRequest(new { error });

        return Ok(result);
    }

    /// <summary>用户注册（默认角色为 Student，可指定 Teacher）</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResult), 201)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        await authProtectionService.AssertRegisterAllowedAsync(GetRegisterSubject(request), request.CaptchaToken, cancellationToken);

        var (result, error) = await registerHandler.HandleAsync(
            new RegisterCommand(
                request.TenantId,
                request.Username,
                request.Password,
                request.Email,
                request.PhoneNumber,
                request.Nickname,
                request.AvatarUrl,
                request.Gender,
                request.Address,
                request.WeChatOpenId,
                request.WeChatUnionId,
                request.Role),
            cancellationToken);

        if (result is null)
            return BadRequest(new { error });

        return StatusCode(201, result);
    }

    /// <summary>获取前端人机校验配置信息（是否启用滑动验证码）。</summary>
    [HttpGet("captcha-config")]
    [ProducesResponseType(200)]
    public IActionResult GetCaptchaConfig()
        => Ok(authProtectionService.GetCaptchaClientConfig());

    /// <summary>生成一道滑动拼图题目（返回背景图、拼图块及元数据）。</summary>
    [HttpGet("captcha")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetCaptchaChallenge(CancellationToken cancellationToken)
    {
        var challenge = await slidingCaptchaService.GenerateChallengeAsync(cancellationToken);
        return Ok(challenge);
    }

    /// <summary>提交滑动位置，验证通过后返回单次使用的 token。</summary>
    [HttpPost("captcha/verify")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyCaptcha([FromBody] CaptchaVerifyRequest request, CancellationToken cancellationToken)
    {
        var token = await slidingCaptchaService.VerifyAndIssueTokenAsync(request.Id, request.X, cancellationToken);
        return Ok(new { token });
    }

    /// <summary>忘记密码 - 申请重置令牌（生产环境将通过邮件发送）</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResult), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await forgotPasswordHandler.HandleAsync(
            new ForgotPasswordCommand(request.Identifier),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>重置密码 - 使用重置令牌设置新密码</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var (success, error) = await resetPasswordHandler.HandleAsync(
            new ResetPasswordCommand(request.ResetToken, request.NewPassword),
            cancellationToken);

        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "密码重置成功，请使用新密码登录。" });
    }

    /// <summary>获取可用租户列表（公开接口，用于注册页租户选择）</summary>
    [HttpGet("tenants")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetPublicTenants(CancellationToken cancellationToken = default)
    {
        var result = await getTenantsHandler.Handle(new GetTenantsQuery(1, 200), cancellationToken);
        var list = result.Items
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Name })
            .ToList();
        return Ok(list);
    }

    private static string GetRegisterSubject(RegisterRequest request)
        => request.Email
            ?? request.PhoneNumber
            ?? request.Username
            ?? $"tenant:{request.TenantId}";
}

public record LoginRequest(string Identifier, string Password, string? CaptchaToken = null);
public record SendCodeRequest(string Target, string? CaptchaToken = null, string? Purpose = null);
public record CodeLoginRequest(string Target, string Code, Guid? TenantId, string? Role, string? Nickname, string? CaptchaToken = null);
public record RegisterRequest(
    Guid? TenantId,
    string? Username,
    string Password,
    string? Email,
    string? PhoneNumber,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    string? Role = null,
    string? CaptchaToken = null);
public record ForgotPasswordRequest(string Identifier);
public record ResetPasswordRequest(string ResetToken, string NewPassword);
public record CaptchaVerifyRequest(string Id, double X);

