namespace ExamSystem.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>生成 JWT 访问令牌</summary>
    string GenerateToken(Guid userId, string displayName, string role, Guid? tenantId);
}
