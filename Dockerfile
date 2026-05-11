# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

WORKDIR /app

# 先复制项目文件以利用层缓存
COPY ExamSystem.slnx ./
COPY src/ExamSystem.Domain/ExamSystem.Domain.csproj           src/ExamSystem.Domain/
COPY src/ExamSystem.Application/ExamSystem.Application.csproj src/ExamSystem.Application/
COPY src/ExamSystem.Infrastructure/ExamSystem.Infrastructure.csproj src/ExamSystem.Infrastructure/
COPY src/ExamSystem.API/ExamSystem.API.csproj                 src/ExamSystem.API/

RUN dotnet restore src/ExamSystem.API/ExamSystem.API.csproj

# 复制剩余源码并发布
COPY src/ src/
RUN dotnet publish src/ExamSystem.API/ExamSystem.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# ca-certificates + tzdata（alpine 镜像默认不含）
RUN apk --no-cache add ca-certificates tzdata

WORKDIR /app

COPY --from=builder /app/publish .
# 嵌入 Amazon RDS 全局 CA 证书（sslmode=verify-full 场景）
COPY global-bundle.pem .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ExamSystem.API.dll"]

