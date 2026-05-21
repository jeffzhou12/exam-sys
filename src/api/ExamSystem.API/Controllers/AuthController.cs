using ExamSystem.Application.Auth.Commands;
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
    GetTenantsQueryHandler getTenantsHandler) : ControllerBase
{
    /// <summary>用户登录，获取 JWT 访问令牌</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(new LoginCommand(request.Username, request.Password), cancellationToken);

        if (result is null)
            return Unauthorized(new { error = "用户名或密码错误。" });

        return Ok(result);
    }

    /// <summary>用户注册（默认角色为 Student）</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResult), 201)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var (result, error) = await registerHandler.HandleAsync(
            new RegisterCommand(request.TenantId, request.Username, request.Password, request.Email),
            cancellationToken);

        if (result is null)
            return BadRequest(new { error });

        return StatusCode(201, result);
    }

    /// <summary>忘记密码 - 申请重置令牌（生产环境将通过邮件发送）</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResult), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await forgotPasswordHandler.HandleAsync(
            new ForgotPasswordCommand(request.UsernameOrEmail),
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
}

public record LoginRequest(string Username, string Password);
public record RegisterRequest(Guid? TenantId, string Username, string Password, string? Email);
public record ForgotPasswordRequest(string UsernameOrEmail);
public record ResetPasswordRequest(string ResetToken, string NewPassword);

