using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
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
        builder.Property(q => q.Content).HasColumnType("text").IsRequired();
        // Options 使用 JSONB 类型
        builder.Property(q => q.Options).HasColumnType("jsonb");
        builder.Property(q => q.CorrectAnswer).HasColumnType("text").IsRequired();
        builder.Property(q => q.Explanation).HasColumnType("text");
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

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Nickname).HasMaxLength(100);
        builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320);
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.WeChatOpenId).HasMaxLength(100).HasColumnName("wechat_openid");
        builder.Property(u => u.WeChatUnionId).HasMaxLength(100).HasColumnName("wechat_unionid");
        builder.Property(u => u.Gender).HasMaxLength(20);
        builder.Property(u => u.Address).HasMaxLength(500);
        builder.Property(u => u.Role)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UserRole>(v))
            .HasMaxLength(50);
        // 同一租户内用户名唯一（系统管理员 tenant_id 为 null，通过数据库约束保证）
        builder.HasIndex(u => new { u.TenantId, u.Username }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.PhoneNumber }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.WeChatOpenId }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.WeChatUnionId }).IsUnique();
        builder.HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .IsRequired(false);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.SenderName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.RecipientName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(4000).IsRequired();
        // AttachedQuestionIds 存储为 JSONB（题目 UUID 数组）
        builder.Property(m => m.AttachedQuestionIds).HasColumnType("jsonb");
        builder.HasIndex(m => new { m.RecipientId, m.IsRead });
        builder.HasIndex(m => m.SenderId);        // ParentMessageId 自引用 FK
        builder.HasOne<Message>().WithMany()
            .HasForeignKey(m => m.ParentMessageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);    }
}

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(300);
        builder.Property(b => b.Publisher).HasMaxLength(300);
        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(1000);
        builder.Property(b => b.PdfFilePath).HasMaxLength(500);
        builder.Property(b => b.Category).HasMaxLength(100);
        builder.Property(b => b.Isbn).HasMaxLength(30);
        builder.Property(b => b.UploadedByName).HasMaxLength(100).IsRequired();
        // Tags 存储为 JSONB（标签字符串数组）
        builder.Property(b => b.Tags).HasColumnType("jsonb");
        builder.HasIndex(b => new { b.TenantId, b.IsActive });
        builder.HasIndex(b => new { b.TenantId, b.Category });
    }
}

public class BookAnnotationConfiguration : IEntityTypeConfiguration<BookAnnotation>
{
    public void Configure(EntityTypeBuilder<BookAnnotation> builder)
    {
        builder.ToTable("book_annotations");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SelectedText).HasMaxLength(2000);
        builder.Property(a => a.Note).HasMaxLength(2000);
        builder.Property(a => a.AiQuestion).HasMaxLength(1000);
        builder.Property(a => a.AiAnswer).HasMaxLength(8000);
        builder.Property(a => a.HighlightColor).HasMaxLength(20).IsRequired();
        // PositionJson 存储为 JSONB（页面位置坐标 {x,y,width,height}）
        builder.Property(a => a.PositionJson).HasColumnType("jsonb");
        builder.HasOne(a => a.Book).WithMany().HasForeignKey(a => a.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => new { a.BookId, a.UserId });
        builder.HasIndex(a => new { a.UserId, a.AnnotationType });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Username).HasMaxLength(100);
        builder.Property(a => a.Role).HasMaxLength(50);
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.RequestPath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.QueryString).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.ErrorMessage).HasMaxLength(2000);
        // OldValues / NewValues 使用 JSONB 存储变更快照
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
        // 审计日志不可变，无 UpdatedAt，忽略 EF Core 自动追踪
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
        builder.HasIndex(a => new { a.UserId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}

public class AiModelConfigConfiguration : IEntityTypeConfiguration<AiModelConfig>
{
    public void Configure(EntityTypeBuilder<AiModelConfig> builder)
    {
        builder.ToTable("ai_model_configs");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ProviderName).HasMaxLength(128).IsRequired();
        builder.Property(c => c.BaseUrl).HasMaxLength(512).IsRequired();
        builder.Property(c => c.ApiKey).HasMaxLength(512).IsRequired();
        builder.Property(c => c.ChatModel).HasMaxLength(256).IsRequired();
        builder.Property(c => c.EmbeddingModel).HasMaxLength(256);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Scene)
            .HasConversion(v => v.ToString(), v => Enum.Parse<AiScene>(v))
            .HasMaxLength(64);
        // TenantId 可为 null（系统级配置），与 Tenant 存在软关联
        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        // 同一租户（或系统级）下同场景可设多个配置，以 Priority 区分
        builder.HasIndex(c => new { c.TenantId, c.Scene, c.IsEnabled });
    }
}

public class PracticeSessionConfiguration : IEntityTypeConfiguration<PracticeSession>
{
    public void Configure(EntityTypeBuilder<PracticeSession> builder)
    {
        builder.ToTable("practice_sessions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.StudentId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.TypeName).HasMaxLength(50);
        builder.Property(p => p.KnowledgePoint).HasMaxLength(200);
        builder.HasIndex(p => new { p.TenantId, p.StudentId, p.CreatedAt });
    }
}

public class SmsTemplateConfiguration : IEntityTypeConfiguration<SmsTemplate>
{
    public void Configure(EntityTypeBuilder<SmsTemplate> builder)
    {
        builder.ToTable("sms_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Scene).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.TemplateBody).HasColumnType("text").IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => new { t.TenantId, t.Scene, t.IsEnabled });
    }
}
