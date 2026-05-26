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
    DbSet<AiModelConfig> AiModelConfigs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Message> Messages { get; }
    DbSet<Book> Books { get; }
    DbSet<BookAnnotation> BookAnnotations { get; }
    DbSet<PracticeSession> PracticeSessions { get; }
    DbSet<SmsTemplate> SmsTemplates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
