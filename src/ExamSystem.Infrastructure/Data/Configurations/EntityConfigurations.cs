using ExamSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamSystem.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.SchemaName).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.SchemaName).IsUnique();
        builder.Property(t => t.ContactEmail).HasMaxLength(320).IsRequired();
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Content).IsRequired();
        // Options 使用 JSONB 类型
        builder.Property(q => q.Options).HasColumnType("jsonb");
        builder.Property(q => q.CorrectAnswer).HasMaxLength(2000).IsRequired();
        builder.HasOne(q => q.Tenant).WithMany(t => t.Questions).HasForeignKey(q => q.TenantId);
        builder.HasIndex(q => new { q.TenantId, q.IsActive });
    }
}

public class ExamPaperConfiguration : IEntityTypeConfiguration<ExamPaper>
{
    public void Configure(EntityTypeBuilder<ExamPaper> builder)
    {
        builder.ToTable("exam_papers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.HasOne(e => e.Tenant).WithMany(t => t.ExamPapers).HasForeignKey(e => e.TenantId);
    }
}

public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("exam_questions");
        builder.HasKey(eq => new { eq.ExamPaperId, eq.QuestionId });
        builder.HasOne(eq => eq.ExamPaper).WithMany(e => e.ExamQuestions).HasForeignKey(eq => eq.ExamPaperId);
        builder.HasOne(eq => eq.Question).WithMany().HasForeignKey(eq => eq.QuestionId);
    }
}

public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.ToTable("student_answers");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.ExamPaper).WithMany(e => e.StudentAnswers).HasForeignKey(s => s.ExamPaperId);
        builder.Property(s => s.StudentId).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => new { s.ExamPaperId, s.StudentId, s.QuestionId }).IsUnique();
    }
}

public class AiAuditLogConfiguration : IEntityTypeConfiguration<AiAuditLog>
{
    public void Configure(EntityTypeBuilder<AiAuditLog> builder)
    {
        builder.ToTable("ai_audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Operation).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ModelName).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
    }
}
