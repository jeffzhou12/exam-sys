# go_web

一个带 PostgreSQL 连接和 GORM Gen 代码生成入口的 Go Web 项目骨架。

## 目录结构

```text
.
├── cmd/
│   ├── gen/             # ORM 代码生成入口
│   └── server/          # Web 服务入口
├── internal/
│   ├── config/          # 配置加载
│   ├── dal/             # gen 输出目录
│   ├── handler/         # HTTP 处理器
│   ├── server/          # HTTP 服务装配
│   └── store/           # 数据库连接
├── go.mod
└── go.sum
```

## 环境变量

程序启动时会自动尝试读取项目根目录下的 `.env` 文件。已经存在的系统环境变量优先级更高，不会被 `.env` 覆盖。

```bash
PORT=8080
DB_HOST=127.0.0.1
DB_PORT=5432
DB_USER=postgres
DB_PASSWORD=postgres
DB_NAME=go_web
DB_SSLMODE=disable
DB_TIMEZONE=UTC
DB_MAX_IDLE_CONNS=10
DB_MAX_OPEN_CONNS=30
DB_CONN_MAX_LIFETIME_MINUTES=30
```

可以先复制 `.env.example` 为 `.env`，再按本地 PostgreSQL 实际配置修改。

## 运行服务

```bash
go run ./cmd/server
```

服务启动时会连接 PostgreSQL，默认监听 :8080，也可以通过环境变量 PORT 覆盖。

## Swagger

启动服务后，可在以下地址查看 Swagger UI：

```text
http://localhost:8080/swagger/
```

当你修改 API 的 Swagger 注释、入参结构或出参结构后，需要重新生成 Swagger 文档产物，因为 [docs/docs.go](docs/docs.go)、[docs/swagger.json](docs/swagger.json) 和 [docs/swagger.yaml](docs/swagger.yaml) 是静态生成文件，不会在运行时自动刷新。

推荐直接使用下面这条命令：

```bash
go generate ./cmd/server
```

它会自动调用本地已安装的 `swag` 命令生成文档，不再依赖 `go run github.com/swaggo/...` 这种容易受模块缓存和 sumdb 路径影响的方式。

首次使用前，只需要安装一次：

```bash
go install github.com/swaggo/swag/cmd/swag@latest
```

如果你希望进一步自动化，常见做法有两种：

1. 在提交前执行 `go generate ./cmd/server && go build ./...`
2. 在 CI 里增加一步校验，确保 Swagger 产物和源码注释一致

## 生成 ORM 代码

```bash
go run ./cmd/gen
```

该命令会读取当前 PostgreSQL 数据库中的所有表，并把生成结果输出到 internal/dal/query 和 internal/dal/model。

## 路由

- `/` 返回纯文本欢迎信息
- `/healthz` 返回应用与数据库健康检查 JSON