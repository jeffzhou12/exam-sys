using ExamSystem.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 租户
        services.AddScoped<Tenants.Queries.GetTenantsQueryHandler>();
        services.AddScoped<Tenants.Commands.CreateTenantCommandHandler>();

        // 题目
        services.AddScoped<Questions.Queries.GetQuestionsQueryHandler>();
        services.AddScoped<Questions.Commands.CreateQuestionCommandHandler>();
        services.AddScoped<Questions.Commands.GenerateQuestionsWithAiCommandHandler>();

        // 试卷
        services.AddScoped<ExamPapers.Queries.GetExamPapersQueryHandler>();
        services.AddScoped<ExamPapers.Commands.CreateExamPaperCommandHandler>();
        services.AddScoped<ExamPapers.Commands.PublishExamPaperCommandHandler>();

        // 答题 & 评分
        services.AddScoped<StudentAnswers.Commands.SubmitAnswersCommandHandler>();
        services.AddScoped<StudentAnswers.Commands.GradeWithAiCommandHandler>();
        services.AddScoped<StudentAnswers.Queries.GetStudentResultQueryHandler>();

        return services;
    }
}
