using ExamSystem.Application.Common.Interfaces;
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
