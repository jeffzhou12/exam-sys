using ExamSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Commands;

// ─── 获取当前用户个人资料 ───────────────────────────────────────────────────

public record GetMyProfileQuery(Guid UserId);

public record MyProfileDto(
    Guid Id, string Username, string? Nickname, string? AvatarUrl,
    string? Email, string? PhoneNumber, string? Gender, string? Address,
    string? EducationLevel, List<string> InterestedSubjects);

public class GetMyProfileQueryHandler(IApplicationDbContext context)
{
    public async Task<MyProfileDto?> Handle(GetMyProfileQuery query, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new MyProfileDto(
                u.Id, u.Username, u.Nickname, u.AvatarUrl,
                u.Email, u.PhoneNumber, u.Gender, u.Address,
                u.EducationLevel, u.InterestedSubjects))
            .FirstOrDefaultAsync(ct);
    }
}

// ─── 更新当前用户个人资料 ───────────────────────────────────────────────────

public record UpdateMyProfileCommand(
    Guid UserId,
    string? Nickname,
    string? AvatarUrl,
    string? Gender,
    string? Address,
    string? EducationLevel,
    List<string>? InterestedSubjects);

public class UpdateMyProfileCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(UpdateMyProfileCommand cmd, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"用户 {cmd.UserId} 不存在。");

        if (cmd.Nickname is not null)         user.Nickname          = cmd.Nickname;
        if (cmd.AvatarUrl is not null)        user.AvatarUrl         = cmd.AvatarUrl;
        if (cmd.Gender is not null)           user.Gender            = cmd.Gender;
        if (cmd.Address is not null)          user.Address           = cmd.Address;
        if (cmd.EducationLevel is not null)   user.EducationLevel    = cmd.EducationLevel;
        if (cmd.InterestedSubjects is not null) user.InterestedSubjects = cmd.InterestedSubjects;

        await context.SaveChangesAsync(ct);
    }
}

// ─── 修改当前用户密码 ───────────────────────────────────────────────────────

public record ChangeMyPasswordCommand(Guid UserId, string OldPassword, string NewPassword);

public class ChangeMyPasswordCommandHandler(IApplicationDbContext context)
{
    private readonly IPasswordHasher<string> _hasher = new PasswordHasher<string>();

    public async Task<(bool Success, string? Error)> Handle(ChangeMyPasswordCommand cmd, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"用户 {cmd.UserId} 不存在。");

        var result = _hasher.VerifyHashedPassword(user.Username, user.PasswordHash, cmd.OldPassword);
        if (result == PasswordVerificationResult.Failed)
            return (false, "当前密码不正确。");

        if (cmd.NewPassword.Length < 6)
            return (false, "新密码长度至少 6 位。");

        user.PasswordHash = _hasher.HashPassword(user.Username, cmd.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return (true, null);
    }
}

// ─── 换绑手机号 ─────────────────────────────────────────────────────────────

public record ChangeMyPhoneCommand(Guid UserId, string NewPhone, string Code);

public class ChangeMyPhoneCommandHandler(
    IApplicationDbContext context,
    IVerificationCodeService codeService)
{
    public async Task<(bool Success, string? Error)> Handle(ChangeMyPhoneCommand cmd, CancellationToken ct = default)
    {
        var valid = await codeService.ValidateAsync(cmd.NewPhone, "change_phone", cmd.Code, ct);
        if (!valid)
            return (false, "验证码错误或已过期。");

        // 检查新手机是否已被其他账号占用
        var conflict = await context.Users.AnyAsync(u => u.PhoneNumber == cmd.NewPhone && u.Id != cmd.UserId, ct);
        if (conflict)
            return (false, "该手机号已被其他账号绑定。");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"用户 {cmd.UserId} 不存在。");

        user.PhoneNumber = cmd.NewPhone;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return (true, null);
    }
}
