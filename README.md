# ExamSystem — 多租户在线考试系统

基于 **.NET 8**、**PostgreSQL 16** 和 **AI (LLM API)** 构建的多租户在线考试系统，部署于 AWS ECS Fargate。

---

## 一、产品设计方案

### 1. 核心业务流程

系统通过"资源池化"与"任务流"的设计思想，将考试全生命周期抽象化：

1. **资源建设**：AI 辅助生成题目 → 进入租户题库。
2. **组卷策略**：设置大纲、难度、AI 随机或精准抽题 → 生成试卷。
3. **考试分发**：设定租户范围内的人员、时间、防作弊开关 → 生成考试任务。
4. **智能阅卷**：客观题自动判分 → 主观题调用 AI 进行语义分析评分。

### 2. 功能模块划分

#### 管理后台 (Admin Portal)

- **多租户管理**：租户入驻、资源配额（AI 调用次数限制）、权限模板。
- **AI 题库管理**：
  - 智能出题：输入知识点，AI 自动生成单选、多选、判断及简答题（JSON 格式入库）。
  - 题目去重：利用向量相似度（Embedding + pgvector）检测重复题目。
- **智能组卷**：固定组卷 / 随机组卷 / AI 均衡组卷。
- **阅卷中心**：AI 辅助判卷（简答题）+ 人工仲裁。

#### 考生前台 (Candidate Portal)

- **考试大厅**：展示可用考试及截止时间。
- **在线答题**：客观题自动评分，简答题异步 AI 评分。
- **结果报告**：得分明细 + AI 评语。

---

## 二、技术架构

### 技术栈

| 层次 | 技术选型 |
|------|----------|
| 后端 | .NET 8 / ASP.NET Core Web API |
| 数据库 | PostgreSQL 16（JSONB + pgvector） |
| 缓存 | Redis（StackExchange.Redis） |
| AI 集成 | OpenAI / Azure OpenAI 兼容接口（可插拔 `IAiService`） |
| 容器 | Docker（multi-stage，alpine 基础镜像） |
| 云服务 | AWS ECS Fargate + ALB + ECR + RDS + Secrets Manager |
| IaC | Terraform |
| CI/CD | GitHub Actions（OIDC，无长期凭证） |

### 项目分层结构

```
exam-system/
├── src/
│   ├── ExamSystem.Domain/          # 领域层（实体、枚举、仓储接口）
│   │   ├── Common/                 # BaseEntity, IRepository<T>
│   │   ├── Entities/               # Tenant, Question, ExamPaper, StudentAnswer, AiAuditLog
│   │   └── Enums/                  # QuestionType, ExamStatus, GradingStatus
│   │
│   ├── ExamSystem.Application/     # 应用层（CQRS 风格 Handler）
│   │   ├── Common/Interfaces/      # IApplicationDbContext, ITenantService, IAiService
│   │   ├── Tenants/                # GetTenantsQuery, CreateTenantCommand
│   │   ├── Questions/              # GetQuestionsQuery, CreateQuestionCommand, GenerateWithAiCommand
│   │   ├── ExamPapers/             # GetExamPapersQuery, CreateExamPaperCommand, PublishExamPaperCommand
│   │   └── StudentAnswers/         # SubmitAnswersCommand, GradeWithAiCommand, GetStudentResultQuery
│   │
│   ├── ExamSystem.Infrastructure/  # 基础设施层
│   │   ├── Configuration/          # DatabaseConfiguration（Secrets Manager + 环境变量）
│   │   ├── Data/                   # ApplicationDbContext, EF Configurations, Migrations
│   │   ├── MultiTenancy/           # TenantService（X-Tenant-ID 请求头解析）
│   │   └── AI/                     # AiService（OpenAI 兼容 REST 调用）
│   │
│   └── ExamSystem.API/             # Web API 层
│       ├── Controllers/            # Health, Tenants, Questions, ExamPapers, StudentAnswers
│       ├── Middleware/             # TenantMiddleware
│       └── Program.cs
│
├── db/init.sql                     # PostgreSQL 初始化脚本（表结构、触发器、Demo租户）
├── terraform/                      # AWS 基础设施（ECR、ECS、ALB、IAM）
├── .github/workflows/deploy.yml    # CI/CD 流水线
├── Dockerfile                      # 多阶段构建（sdk:8.0-alpine → aspnet:8.0-alpine）
└── ExamSystem.slnx                 # .NET 解决方案文件
```

---

## 三、多租户设计

每个租户通过 HTTP 请求头 `X-Tenant-ID: <uuid>` 标识。`TenantMiddleware` 在请求进入业务层前验证租户合法性。所有业务表均含 `tenant_id` 字段，EF Core 查询时自动过滤（行级隔离）。

---

## 四、数据库配置

### 环境变量约定（与 ECS 任务定义一致）

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `DB_HOST` | RDS 实例地址 | `db.xxx.rds.amazonaws.com` |
| `DB_PORT` | 端口（默认 5432） | `5432` |
| `DB_USER` | 用户名 | `postgres` |
| `DB_NAME` | 数据库名 | `exam_system` |
| `DB_SSLMODE` | SSL 模式 | `verify-full`（生产）/ `disable`（本地） |
| `DB_SSL_ROOT_CERT` | CA 证书路径 | `/app/global-bundle.pem` |
| `DB_PASSWORD_SECRET_ARN` | Secrets Manager ARN（生产） | `arn:aws:secretsmanager:ap-southeast-1:...` |
| `DB_PASSWORD` | 明文密码（本地开发 fallback） | `postgres` |

`DB_HOST` 存在时使用环境变量，否则 fallback 到 `appsettings.json` 中的 `ConnectionStrings:DefaultConnection`。

### 初始化数据库

```bash
# EF Core 迁移（推荐）
dotnet ef database update \
  --project src/ExamSystem.Infrastructure \
  --startup-project src/ExamSystem.API

# 或手动执行 SQL 脚本
psql -U postgres -d exam_system -f db/init.sql
```

---

## 五、本地开发

**前置条件**：[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)、PostgreSQL 16、Redis（可选）

```powershell
# 1. 设置环境变量
$env:DB_HOST     = "localhost"
$env:DB_USER     = "postgres"
$env:DB_PASSWORD = "postgres"
$env:DB_NAME     = "exam_system_dev"

# 可选 AI 服务
$env:AI_API_KEY  = "sk-xxxxxxxxxxxx"
$env:AI_BASE_URL = "https://api.openai.com/v1"

# 2. 创建数据库并应用迁移
createdb -U postgres exam_system_dev
dotnet ef database update --project src/ExamSystem.Infrastructure --startup-project src/ExamSystem.API

# 3. 启动
dotnet run --project src/ExamSystem.API
# Swagger UI: http://localhost:8080/swagger
```

---

## 六、API 端点一览

所有业务接口需携带 `X-Tenant-ID: <uuid>` 请求头（`/health`、`/swagger`、`/api/tenants` 除外）。

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/health` | 健康检查 |
| GET | `/api/tenants` | 获取租户列表 |
| POST | `/api/tenants` | 创建租户 |
| GET | `/api/questions` | 获取题目列表（分页+过滤） |
| POST | `/api/questions` | 手动创建题目 |
| POST | `/api/questions/ai-generate` | AI 自动生成题目 |
| GET | `/api/exam-papers` | 获取试卷列表 |
| POST | `/api/exam-papers` | 创建试卷 |
| POST | `/api/exam-papers/{id}/publish` | 发布试卷 |
| POST | `/api/exam-papers/{id}/answers` | 提交答案（客观题自动评分） |
| POST | `/api/exam-papers/{id}/answers/{studentId}/grade-ai` | AI 评阅简答题 |
| GET | `/api/exam-papers/{id}/answers/{studentId}` | 查询成绩报告 |

---

## 七、Docker 本地运行

```bash
docker build -t exam-system:local .

docker run -p 8080:8080 \
  -e DB_HOST=host.docker.internal \
  -e DB_USER=postgres \
  -e DB_PASSWORD=postgres \
  -e DB_NAME=exam_system_dev \
  -e DB_SSLMODE=disable \
  exam-system:local
```

---

## 八、部署（AWS ECS Fargate）

### 1. Terraform 初始化

```bash
cd terraform/environments/prod
cp terraform.tfvars.example terraform.tfvars
# 填入实际值后执行：
terraform init && terraform plan && terraform apply
```

### 2. GitHub Actions CI/CD

推送到 `main` 分支自动触发：Build → ECR Push → EF 迁移（ECS RunTask）→ ECS 滚动部署

需在仓库 Secrets 中配置：

| Secret | 说明 |
|--------|------|
| `AWS_DEPLOY_ROLE_ARN` | OIDC IAM Role ARN |
| `ECS_SUBNET_ID` | 迁移任务子网 ID |
| `ECS_SG_ID` | 迁移任务安全组 ID |


## 九、实施路径

| 阶段 | 状态 | 内容 |
|------|------|------|
| MVP | 完成 | 多租户基础架构、手动出题/组卷、客观题自动评分 |
| AI 集成 | 完成 | AI 自动出题（OpenAI 兼容接口）、简答题 AI 评分 |
| 增强 | 规划中 | pgvector 题目去重、AI 能力报告、防作弊监控 |