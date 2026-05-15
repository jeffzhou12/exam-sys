using ExamSystem.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 租户
        services.AddScoped<Tenants.Queries.GetTenantsQueryHandler>();
        services.AddScoped<Tenants.Queries.GetTenantByIdQueryHandler>();
        services.AddScoped<Tenants.Commands.CreateTenantCommandHandler>();
        services.AddScoped<Tenants.Commands.UpdateTenantCommandHandler>();
        services.AddScoped<Tenants.Commands.ToggleTenantStatusCommandHandler>();

        // 题目
        services.AddScoped<Questions.Queries.GetQuestionsQueryHandler>();
        services.AddScoped<Questions.Queries.GetQuestionByIdQueryHandler>();
        services.AddScoped<Questions.Commands.CreateQuestionCommandHandler>();
        services.AddScoped<Questions.Commands.UpdateQuestionCommandHandler>();
        services.AddScoped<Questions.Commands.DeleteQuestionCommandHandler>();
        services.AddScoped<Questions.Commands.GenerateQuestionsWithAiCommandHandler>();
        services.AddScoped<Questions.Commands.PreviewAiQuestionsCommandHandler>();
        services.AddScoped<Questions.Commands.BatchCreateQuestionsCommandHandler>();

        // 试卷
        services.AddScoped<ExamPapers.Queries.GetExamPapersQueryHandler>();
        services.AddScoped<ExamPapers.Queries.GetExamPaperDetailQueryHandler>();
        services.AddScoped<ExamPapers.Queries.GetExamResultsQueryHandler>();
        services.AddScoped<ExamPapers.Commands.CreateExamPaperCommandHandler>();
        services.AddScoped<ExamPapers.Commands.UpdateExamPaperCommandHandler>();
        services.AddScoped<ExamPapers.Commands.PublishExamPaperCommandHandler>();
        services.AddScoped<ExamPapers.Commands.CancelExamPaperCommandHandler>();

        // 答题 & 评分
        services.AddScoped<StudentAnswers.Commands.SubmitAnswersCommandHandler>();
        services.AddScoped<StudentAnswers.Commands.GradeWithAiCommandHandler>();
        services.AddScoped<StudentAnswers.Commands.ManualGradeCommandHandler>();
        services.AddScoped<StudentAnswers.Queries.GetStudentResultQueryHandler>();
        services.AddScoped<StudentAnswers.Queries.GetStudentExamsQueryHandler>();

        // 用户管理
        services.AddScoped<Users.Queries.GetUsersQueryHandler>();
        services.AddScoped<Users.Queries.GetUserByIdQueryHandler>();
        services.AddScoped<Users.Commands.CreateUserCommandHandler>();
        services.AddScoped<Users.Commands.UpdateUserCommandHandler>();
        services.AddScoped<Users.Commands.ToggleUserStatusCommandHandler>();
        services.AddScoped<Users.Commands.AdminResetPasswordCommandHandler>();

        // 在线练习
        services.AddScoped<Practice.GetPracticeQuestionsQueryHandler>();
        services.AddScoped<Practice.SubmitPracticeCommandHandler>();
        services.AddScoped<Practice.GetSimilarQuestionsQueryHandler>();
        services.AddScoped<Practice.ExplainQuestionCommandHandler>();

        // 站内信
        services.AddScoped<Messages.SendMessageCommandHandler>();
        services.AddScoped<Messages.GetMessagesQueryHandler>();
        services.AddScoped<Messages.MarkMessageReadCommandHandler>();
        services.AddScoped<Messages.GetTenantTeachersQueryHandler>();

        // 图书
        services.AddScoped<Books.GetBooksQueryHandler>();
        services.AddScoped<Books.GetBookByIdQueryHandler>();
        services.AddScoped<Books.CreateBookCommandHandler>();
        services.AddScoped<Books.UpdateBookCommandHandler>();
        services.AddScoped<Books.UploadBookPdfCommandHandler>();
        services.AddScoped<Books.DeleteBookCommandHandler>();
        services.AddScoped<Books.GetBookAnnotationsQueryHandler>();
        services.AddScoped<Books.CreateAnnotationCommandHandler>();
        services.AddScoped<Books.UpdateAnnotationCommandHandler>();
        services.AddScoped<Books.DeleteAnnotationCommandHandler>();
        services.AddScoped<Books.AiAnalyzeTextCommandHandler>();

        // 审计日志
        services.AddScoped<AuditLogs.GetAuditLogsQueryHandler>();

        return services;
    }
}

