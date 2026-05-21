using System.Text;
using System.Text.Json;
using ExamSystem.API;
using ExamSystem.Application;
using ExamSystem.Application.Common.Models;
using ExamSystem.Infrastructure;
using ExamSystem.API.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .Enrich.WithThreadId());

// ── JWT 配置 ──────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");

// 支持通过环境变量覆盖 SecretKey（生产环境由 ECS 注入）
var secretKeyFromEnv = Environment.GetEnvironmentVariable("JWT__SECRETKEY")
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(secretKeyFromEnv))
    jwtSettings = jwtSettings with { SecretKey = secretKeyFromEnv };

// ── 服务注册 ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // 使用统一格式覆盖 FluentValidation 触发的自动 400 响应
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                title  = "请求参数验证失败",
                status = 400,
                errors
            });
        };
    });

// FluentValidation 自动验证
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ExamSystem API", Version = "v1" });

    // X-Tenant-ID 全局 Header 参数
    c.AddSecurityDefinition("TenantId", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-ID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "多租户标识，格式为 UUID（例：00000000-0000-0000-0000-000000000001）"
    });

    // JWT Bearer 授权
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "请输入 JWT Token（不需要加 'Bearer ' 前缀）"
    });

    // Bearer 全局要求（所有接口），TenantId 由 TenantIdOperationFilter 按需追加（跳过匿名接口）
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
    c.OperationFilter<TenantIdOperationFilter>();

    // JsonDocument 在 Swagger 中显示为自由 JSON 对象，避免生成 additionalProp1/2/3
    c.MapType<JsonDocument>(() => new OpenApiSchema
    {
        Type = "object",
        Nullable = true,
        Description = "任意 JSON 对象",
        Example = new OpenApiObject()
    });
});

// ── JWT 认证 ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// 分层服务注册
builder.Services.AddHttpContextAccessor(); // 供 TenantService 使用
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 大文件上传限制（PDF 最大 200 MB）
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 200 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 200 * 1024 * 1024);

var app = builder.Build();

// ── 中间件管道 ────────────────────────────────────────────────────────────────
// 1. 全局异常处理（最外层，捕获所有后续中间件的异常）
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. 请求审计日志（次外层，记录请求进入和响应状态）
app.UseMiddleware<RequestAuditMiddleware>();

// 开发环境：为本地存储的上传文件（封面图等）提供静态文件服务
if (app.Environment.IsDevelopment())
{
    var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExamSystem API v1");
    c.DisplayRequestDuration();
});

app.UseAuthentication();

// 多租户中间件（在认证之后、授权之前）
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("ExamSystem API 启动成功，环境：{Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}
