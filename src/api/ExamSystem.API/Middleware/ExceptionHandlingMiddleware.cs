using System.Net;
using System.Text.Json;
using ExamSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Middleware;

/// <summary>
/// 全局异常处理中间件：捕获所有未处理异常，返回统一 ProblemDetails 格式响应，并通过 Serilog 记录结构化日志。
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = MapException(exception);

        // 5xx 用 Error 级别，4xx 用 Warning 级别
        if (statusCode >= 500)
        {
            logger.LogError(exception,
                "未处理异常 [{ExceptionType}] | {Method} {Path} | TraceId={TraceId}",
                exception.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                "业务异常 [{ExceptionType}] {Detail} | {Method} {Path} | TraceId={TraceId}",
                exception.GetType().Name,
                detail,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            FileNotFoundException e =>
                (StatusCodes.Status404NotFound, "文件不存在", e.Message.NullIfEmpty() ?? "请求的文件未找到。"),

            UnauthorizedAccessException e =>
                (StatusCodes.Status401Unauthorized, "未授权", e.Message.NullIfEmpty() ?? "您无权执行此操作。"),

            TooManyRequestsException e =>
                (StatusCodes.Status429TooManyRequests, "请求过于频繁", e.Message.NullIfEmpty() ?? "请求过于频繁，请稍后重试。"),

            KeyNotFoundException e =>
                (StatusCodes.Status404NotFound, "资源不存在", e.Message.NullIfEmpty() ?? "请求的资源未找到。"),

            ArgumentNullException e =>
                (StatusCodes.Status400BadRequest, "参数错误", $"缺少必要参数：{e.ParamName}。"),

            ArgumentException e =>
                (StatusCodes.Status400BadRequest, "参数错误", e.Message.NullIfEmpty() ?? "请求参数无效。"),

            InvalidOperationException e =>
                (StatusCodes.Status400BadRequest, "操作无效", e.Message.NullIfEmpty() ?? "当前状态下不允许此操作。"),

            OperationCanceledException =>
                (StatusCodes.Status408RequestTimeout, "请求超时", "请求已被取消或超时，请重试。"),

            NotSupportedException e =>
                (StatusCodes.Status400BadRequest, "不支持的操作", e.Message.NullIfEmpty() ?? "该操作暂不支持。"),

            _ =>
                (StatusCodes.Status500InternalServerError, "服务器内部错误", "服务器发生了意外错误，请稍后重试。")
        };
}

file static class StringExtensions
{
    internal static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
