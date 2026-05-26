using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Application.Common.Models;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Queries;

public record GetUsersQuery(
    Guid? TenantId,
    int Page = 1,
    int PageSize = 20,
    UserRole? Role = null,
    bool? IsActive = null,
    string? Search = null);

public record UserDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    string Username,
    string? Nickname,
    string? AvatarUrl,
    string? Email,
    string? PhoneNumber,
    string? Gender,
    string? Address,
    string? WeChatOpenId,
    string? WeChatUnionId,
    UserRole Role,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public class GetUsersQueryHandler(IApplicationDbContext context)
{
    public async Task<PaginatedResult<UserDto>> Handle(
        GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .AsQueryable();

        if (query.TenantId.HasValue)
            q = q.Where(u => u.TenantId == query.TenantId.Value);

        if (query.Role.HasValue)
            q = q.Where(u => u.Role == query.Role.Value);

        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(u => u.Username.ToLower().Contains(search)
                           || (u.Nickname != null && u.Nickname.ToLower().Contains(search))
                           || (u.Email != null && u.Email.ToLower().Contains(search))
                           || (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(search)));
        }

        var totalCount = await q.LongCountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserDto(
                u.Id, u.TenantId, u.Tenant != null ? u.Tenant.Name : null,
                u.Username, u.Nickname, u.AvatarUrl, u.Email, u.PhoneNumber, u.Gender, u.Address, u.WeChatOpenId, u.WeChatUnionId,
                u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return PaginatedResult<UserDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}
