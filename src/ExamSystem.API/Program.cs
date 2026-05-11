using ExamSystem.Application;
using ExamSystem.Infrastructure;
using ExamSystem.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── 服务注册 ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ExamSystem API", Version = "v1" });
});

// 分层服务注册
builder.Services.AddHttpContextAccessor(); // 供 TenantService 使用
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── 中间件管道 ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 多租户中间件（需在授权之前）
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
