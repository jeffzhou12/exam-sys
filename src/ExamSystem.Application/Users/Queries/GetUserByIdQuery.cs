using ExamSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Users.Queries;

public record GetUserByIdQuery(Guid UserId);

public class GetUserByIdQueryHandler(IApplicationDbContext context)
{
    public async Task<UserDto?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserDto(
                u.Id, u.TenantId, u.Tenant != null ? u.Tenant.Name : null,
                u.Username, u.Email, u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
