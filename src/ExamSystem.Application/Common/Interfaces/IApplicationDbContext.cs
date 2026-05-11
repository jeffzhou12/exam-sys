using ExamSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Question> Questions { get; }
    DbSet<ExamPaper> ExamPapers { get; }
    DbSet<ExamQuestion> ExamQuestions { get; }
    DbSet<StudentAnswer> StudentAnswers { get; }
    DbSet<AiAuditLog> AiAuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
