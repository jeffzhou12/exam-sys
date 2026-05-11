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

        // 用户管理
        services.AddScoped<Users.Queries.GetUsersQueryHandler>();
        services.AddScoped<Users.Queries.GetUserByIdQueryHandler>();
        services.AddScoped<Users.Commands.CreateUserCommandHandler>();
        services.AddScoped<Users.Commands.UpdateUserCommandHandler>();
        services.AddScoped<Users.Commands.ToggleUserStatusCommandHandler>();
        services.AddScoped<Users.Commands.AdminResetPasswordCommandHandler>();

        return services;
    }
}

