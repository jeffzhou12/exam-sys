# ExamSystem — 多租户在线考试系统

基于 **.NET 8**、**Vue 3**、**PostgreSQL 16** 和 **AI (LLM API)** 构建的多租户在线考试系统，部署于 AWS ECS Fargate + CloudFront。

---

## 一、产品设计方案

### 1. 核心业务流程

1. **资源建设**：AI 辅助生成题目 → 进入租户题库
2. **组卷策略**：设置大纲、难度、AI 随机或精准抽题 → 生成试卷
3. **考试分发**：设定租户范围内的人员、时间、防作弊开关 → 生成考试任务
4. **智能阅卷**：客观题自动判分 → 主观题调用 AI 进行语义分析评分

### 2. 功能模块划分

#### 管理后台 (Admin — Vue 3，端口 3000)

- **多租户管理**：租户入驻、资源配额、权限模板
- **用户管理**：按租户隔离的用户列表（SuperAdmin 可跨租户查看）
- **AI 题库管理**：智能出题、题目 CRUD、分页过滤
- **智能组卷**：固定组卷 / 随机组卷 / AI 均衡组卷
- **阅卷中心**：AI 辅助判卷（简答题）+ 人工仲裁

#### 考生前台 (Portal — Vue 3，端口 3001)

- **公开考试列表**：匿名可浏览所有已发布试卷（无需登录）
- **登录 / 注册**：JWT 认证，纳入统一 PortalLayout（含 header/footer）
- **考试详情**：须登录后方可查看完整题目（`requiresAuth`）
- **答题室**：实时作答、倒计时、提交答卷
- **成绩查询**：查看个人历史考试成绩与详细评分

#### 角色权限

| 角色 | 说明 |
|------|------|
| SuperAdmin (`role=-1`) | 全局管理员，无租户隔离，可管理所有租户和用户 |
| Admin | 租户管理员，管理本租户内所有资源 |
| Student | 考生，只能查看/提交自己的答卷 |

---

## 二、技术架构

### 技术栈

| 层次 | 技术选型 |
|------|----------|
| 后端 | .NET 8 / ASP.NET Core Web API（Clean Architecture） |
| 前端 | Vue 3.4 + Element Plus + Pinia + Vue Router 4 + Vite 5（Admin 管理后台 + Portal 考生前台） |
| 数据库 | PostgreSQL 16（JSONB） |
| 缓存 | Redis（ElastiCache，StackExchange.Redis） |
| AI 集成 | DeepSeek / 硅基流动（OpenAI 兼容接口，主备切换） |
| 容器 | Docker（multi-stage，alpine 基础镜像），端口 8080 |
| 云服务 | AWS ECS Fargate + ALB + ECR + RDS + Secrets Manager + CloudFront + S3 |
| IaC | Terraform >= 1.7 |
| CI/CD | GitHub Actions（OIDC，无长期凭证） |

### 项目目录结构

```
exam-system/
├── src/
│   ├── ExamSystem.Domain/          # 领域层（实体、枚举、仓储接口）
│   │   ├── Common/                 # BaseEntity, IRepository<T>
│   │   ├── Entities/               # Tenant, User, Question, ExamPaper, StudentAnswer, AiAuditLog
│   │   └── Enums/                  # QuestionType, ExamStatus, GradingStatus
│   │
│   ├── ExamSystem.Application/     # 应用层（CQRS 风格 Handler）
│   │   ├── Common/                 # PaginatedResult<T>, ITenantService, IAiService
│   │   ├── Auth/                   # LoginCommand
│   │   ├── Tenants/                # GetTenantsQuery, CreateTenantCommand
│   │   ├── Users/                  # GetUsersQuery, CreateUserCommand
│   │   ├── Questions/              # CRUD + GenerateWithAiCommand
│   │   ├── ExamPapers/             # CRUD + PublishExamPaperCommand
│   │   └── StudentAnswers/         # SubmitAnswersCommand, GradeWithAiCommand
│   │
│   ├── ExamSystem.Infrastructure/  # 基础设施层
│   │   ├── Configuration/          # DatabaseConfiguration（Secrets Manager + 环境变量）
│   │   ├── Data/                   # ApplicationDbContext, EF Migrations
│   │   ├── MultiTenancy/           # TenantService（X-Tenant-ID 请求头解析）
│   │   ├── Auth/                   # LoginCommandHandler（JWT HS256）
│   │   ├── Caching/                # Redis 缓存服务
│   │   └── AI/                     # AiService（主备 Provider 切换）
│   │
│   └── ExamSystem.API/             # Web API 层（端口 8080）
│       ├── Controllers/            # Auth, Health, Tenants, Users, Questions, ExamPapers, StudentAnswers
│       ├── Middleware/             # TenantMiddleware（租户验证）
│       └── Program.cs
│
│   ├── src/web/admin/                  # 管理后台前端（Vue 3，端口 3000）
│   │   ├── src/
│   │   │   ├── views/                  # Login, Dashboard, Tenants, Users, Questions, ExamPapers
│   │   │   ├── stores/                 # Pinia（auth, tenant）
│   │   │   ├── router/                 # 路由（角色权限守卫）
│   │   │   └── api/                    # Axios 封装（自动注入 X-Tenant-ID）
│   │   └── .env.production             # VITE_API_BASE_URL=（留空，使用相对路径 /api）
│   │
│   ├── src/web/portal/                 # 考生前台（Vue 3，端口 3001）
│   │   ├── src/
│   │   │   ├── views/                  # Home, ExamList, ExamDetail, ExamRoom, Login, Register, MyResults
│   │   │   ├── layouts/                # PortalLayout（header + footer 统一布局）
│   │   │   ├── stores/                 # Pinia（auth）
│   │   │   ├── router/                 # 路由（requiresAuth 守卫，Login/Register 嵌入 PortalLayout）
│   │   │   └── api/                    # Axios 封装（withTenant 标志按需注入 X-Tenant-ID）
│   │   └── vite.config.js              # 开发代理 /api → http://localhost:5146
│   │
│   └── src/app/                        # 移动端 App（Ionic + Vue + Capacitor）
│
├── terraform/
│   ├── modules/                    # alb, ecr, ecs, elasticache, iam, cloudfront
│   └── environments/prod/          # 生产环境变量（terraform.tfvars）
│
├── .github/workflows/deploy.yml    # CI/CD 流水线（路径感知，分离后端/前端部署）
├── db/init.sql                     # PostgreSQL 初始化脚本
├── Dockerfile                      # 多阶段构建（sdk:8.0-alpine → aspnet:8.0-alpine）
└── ExamSystem.slnx                 # .NET 解决方案文件
```

---

## 三、多租户设计

每个请求通过 HTTP 头 `X-Tenant-ID: <uuid>` 标识租户。`TenantMiddleware` 在请求进入业务层前验证租户合法性。所有业务表均含 `tenant_id` 字段，EF Core 查询时自动过滤（行级隔离）。

**公开路径**（无需 X-Tenant-ID）：`/healthz`、`/health`、`/swagger`、`/api/auth`、`/api/tenants`、`/api/users`

**公开 API**（无需登录，支持匿名访问）：`GET /api/exam-papers`、`GET /api/exam-papers/{id}`  
后端通过 `[AllowAnonymous]` + `TryGetCurrentTenantId()`（无租户头时返回 null 而非抛异常）实现。

**SuperAdmin**（`role = -1`）不绑定任何租户，拥有全局权限，TenantMiddleware 自动跳过租户校验。

---

## 四、本地开发

**前置条件**：[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)、Node.js 20+、PostgreSQL 16、Redis（可选）

### 后端

```powershell
# 1. 配置环境变量
$env:DB_HOST     = "localhost"
$env:DB_USER     = "postgres"
$env:DB_PASSWORD = "postgres"
$env:DB_NAME     = "exam_system_dev"

# 2. 应用数据库迁移
dotnet ef database update `
  --project src/ExamSystem.Infrastructure `
  --startup-project src/ExamSystem.API

# 3. 启动（http://localhost:8080/swagger）
dotnet run --project src/ExamSystem.API
```

### 前端

```powershell
# 管理后台（http://localhost:3000）
cd src/web/admin
npm install
npm run dev

# 考生前台（http://localhost:3001）
cd src/web/portal
npm install
npm run dev
```

---

## 五、API 端点一览

所有业务接口需携带 `Authorization: Bearer <token>` 和 `X-Tenant-ID: <uuid>`（公开路径除外）。

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/healthz` | ALB 健康检查 |
| POST | `/api/auth/login` | 登录，返回 JWT |
| GET | `/api/tenants` | 获取租户列表 |
| POST | `/api/tenants` | 创建租户 |
| GET | `/api/users` | 获取用户列表（按租户隔离） |
| POST | `/api/users` | 创建用户 |
| GET | `/api/questions` | 获取题目列表（分页+过滤） |
| POST | `/api/questions` | 手动创建题目 |
| POST | `/api/questions/ai-generate` | AI 自动生成题目 |
| GET | `/api/exam-papers` | 获取试卷列表 |
| POST | `/api/exam-papers` | 创建试卷 |
| POST | `/api/exam-papers/{id}/publish` | 发布试卷 |
| POST | `/api/student-answers` | 提交答案（客观题自动评分） |
| POST | `/api/student-answers/{id}/grade-ai` | AI 评阅简答题 |

完整文档见 Swagger UI：`http://localhost:8080/swagger`（本地）

---

## 六、Docker 本地运行

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

## 七、AWS 部署架构

```
用户浏览器
    │
    ├─── Admin 管理后台
    │       ▼
    │   CloudFront (https://<admin>.cloudfront.net)
    │       ├── /*        → S3（Admin Vue 3 SPA 静态资源）
    │       └── /api/*    → ALB → ECS Fargate（.NET 8 API，端口 8080）
    │
    └─── Portal 考生前台
            ▼
        CloudFront (https://<portal>.cloudfront.net)
            ├── /*        → S3（Portal Vue 3 SPA 静态资源）
            └── /api/*    → ALB → ECS Fargate（.NET 8 API，端口 8080）
                                      │
                            ┌─────────┴─────────┐
                            ▼                   ▼
                      RDS PostgreSQL      ElastiCache Redis
                      (ap-southeast-1)   (ap-southeast-1)
```

### 数据库环境变量（ECS 任务注入）

| 变量名 | 说明 |
|--------|------|
| `DB_HOST` | RDS 实例地址 |
| `DB_PORT` | 端口（默认 5432） |
| `DB_USER` | 用户名 |
| `DB_NAME` | 数据库名 |
| `DB_SSLMODE` | `verify-full`（生产）/ `disable`（本地） |
| `DB_SSL_ROOT_CERT` | CA 证书路径（`/app/global-bundle.pem`） |
| `DB_PASSWORD_SECRET_ARN` | AWS Secrets Manager ARN |
| `REDIS_CONNECTION` | ElastiCache 连接字符串 |
| `JWT__SECRETKEY` | JWT 签名密钥 |
| `AI__PRIMARY__APIKEY` | 主力 AI Provider Key |
| `AI__FALLBACK__APIKEY` | 备用 AI Provider Key |

### AI 服务配置（双 Provider）

| 角色 | Provider | 模型 |
|------|----------|------|
| 主力 | DeepSeek 官方 | deepseek-chat |
| 备用 | 硅基流动 | deepseek-ai/DeepSeek-V3 |

---

## 八、基础设施管理（Terraform）

```bash
cd terraform/environments/prod

terraform init
terraform plan
terraform apply
```

### 主要输出

| 输出 | 当前值 |
|------|--------|
| `frontend_url` | https://d165uf1arxthuu.cloudfront.net |
| `frontend_s3_bucket` | `exam-prod-frontend` |
| `cloudfront_distribution_id` | `E2ZM87T9HC3XX0` |
| `portal_url` | https://d2a95y8zxdxfqo.cloudfront.net |
| `portal_s3_bucket` | `exam-prod-portal` |
| `portal_cloudfront_distribution_id` | `ETKCW0WLJ5CFJ` |
| `alb_dns_name` | `http://exam-prod-1791453538.ap-southeast-1.elb.amazonaws.com` |
| `ecr_repository_url` | `183047559773.dkr.ecr.ap-southeast-1.amazonaws.com/exam-prod` |
| `github_actions_role_arn` | `arn:aws:iam::183047559773:role/exam-prod-github-actions` |

---

## 九、CI/CD 流水线（GitHub Actions）

推送到 `main` 分支自动触发，**路径感知**（只部署有变更的部分）：

| 触发路径 | 执行 Job |
|----------|----------|
| `src/**`, `Dockerfile`, `db/**` | Build → ECR Push → EF 迁移（`ecs run-task`）→ ECS 滚动部署 |
| `src/web/admin/**` | `npm run build` → S3 sync → CloudFront 缓存失效（Admin） |
| `src/web/portal/**` | `npm run build` → S3 sync → CloudFront 缓存失效（Portal） |

支持 `workflow_dispatch` 手动触发，可通过 `force_backend` / `force_frontend` / `force_portal` 强制指定部署目标。

### 所需 GitHub Secrets

| Secret | 说明 |
|--------|------|
| `AWS_DEPLOY_ROLE_ARN` | OIDC IAM Role ARN（`terraform output github_actions_role_arn`） |
| `ECS_SUBNET_ID` | ECS 迁移任务子网 ID |
| `ECS_SG_ID` | ECS 迁移任务安全组 ID |
| `S3_BUCKET_NAME` | Admin 前端 S3 存储桶名称（`terraform output frontend_s3_bucket`） |
| `CLOUDFRONT_DISTRIBUTION_ID` | Admin CloudFront Distribution ID（`terraform output cloudfront_distribution_id`） |
| `S3_BUCKET_NAME_PORTAL` | Portal 前端 S3 存储桶名称（`terraform output portal_s3_bucket`） |
| `CLOUDFRONT_DISTRIBUTION_ID_PORTAL` | Portal CloudFront Distribution ID（`terraform output portal_cloudfront_distribution_id`） |

---

## 十、实施路径

| 阶段 | 状态 | 内容 |
|------|------|------|
| MVP | ✅ 完成 | 多租户基础架构、手动出题/组卷、客观题自动评分 |
| AI 集成 | ✅ 完成 | AI 自动出题（双 Provider）、简答题 AI 评分 |
| 管理前端 | ✅ 完成 | Vue 3 管理后台，CloudFront + S3 部署 |
| 考生前台 | ✅ 完成 | Vue 3 Portal（匿名浏览、登录注册、答题室、成绩查询），独立 CloudFront + S3 部署 |
| CI/CD | ✅ 完成 | GitHub Actions 路径感知自动部署（后端 + Admin + Portal 三路径独立触发） |
| 增强 | 🔄 规划中 | pgvector 题目去重、AI 能力报告、防作弊监控 |
