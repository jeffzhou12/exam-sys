# ── ECS Service Linked Role ───────────────────────────────────────────────────
# Required for ECS capacity providers. Created automatically on first ECS use,
# but we manage it here so Terraform is aware of it.
# If it already exists, import it:
#   terraform import module.ecs.aws_iam_service_linked_role.ecs \
#     arn:aws:iam::<ACCOUNT_ID>:role/aws-service-role/ecs.amazonaws.com/AWSServiceRoleForECS
resource "aws_iam_service_linked_role" "ecs" {
  aws_service_name = "ecs.amazonaws.com"

  lifecycle {
    # Ignore if the role already exists in the account
    ignore_changes = [description]
  }
}

# ── CloudWatch Log Group ──────────────────────────────────────────────────────
resource "aws_cloudwatch_log_group" "this" {
  name              = "/ecs/${var.name}"
  retention_in_days = 30
  tags              = var.tags
}

# ── ECS Tasks Security Group ──────────────────────────────────────────────────
resource "aws_security_group" "ecs_tasks" {
  name        = "${var.name}-ecs-tasks"
  description = "Allow traffic from ALB to ECS tasks; allow all egress"
  vpc_id      = var.vpc_id

  ingress {
    description     = "From ALB"
    from_port       = var.app_port
    to_port         = var.app_port
    protocol        = "tcp"
    security_groups = [var.alb_security_group_id]
  }

  egress {
    description = "Allow all outbound (Secrets Manager, RDS, ECR, CloudWatch)"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, { Name = "${var.name}-ecs-tasks" })
}

# ── ECS Cluster ───────────────────────────────────────────────────────────────
resource "aws_ecs_cluster" "this" {
  name = var.name

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = var.tags
}

resource "aws_ecs_cluster_capacity_providers" "this" {
  cluster_name       = aws_ecs_cluster.this.name
  capacity_providers = ["FARGATE", "FARGATE_SPOT"]

  default_capacity_provider_strategy {
    capacity_provider = "FARGATE"
    weight            = 1
  }

  depends_on = [aws_iam_service_linked_role.ecs]
}

# ── ECS Task Definition ───────────────────────────────────────────────────────
resource "aws_ecs_task_definition" "this" {
  family                   = var.name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory
  execution_role_arn       = var.execution_role_arn
  task_role_arn            = var.task_role_arn

  container_definitions = jsonencode([{
    name      = var.name
    image     = var.container_image
    essential = true

    portMappings = [{
      containerPort = var.app_port
      protocol      = "tcp"
    }]

    environment = [
      { name = "PORT",                   value = tostring(var.app_port) },
      { name = "DB_HOST",                value = var.db_host },
      { name = "DB_PORT",                value = "5432" },
      { name = "DB_USER",                value = var.db_user },
      { name = "DB_NAME",                value = var.db_name },
      { name = "DB_SSLMODE",             value = "verify-full" },
      { name = "DB_SSL_ROOT_CERT",       value = "/app/global-bundle.pem" },
      { name = "DB_PASSWORD_SECRET_ARN", value = var.db_secret_arn },
      { name = "DB_TIMEZONE",            value = "UTC" },
      { name = "DB_MAX_POOL_SIZE",       value = "20" },
      { name = "DB_MIN_POOL_SIZE",       value = "2" },
      # .NET / ASP.NET Core
      { name = "ASPNETCORE_URLS",        value = "http://+:${var.app_port}" },
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      # AI 服务 - 主 Provider（DeepSeek 官方）
      { name = "AI__PRIMARY__APIKEY",      value = var.ai_primary_api_key },
      { name = "AI__PRIMARY__BASEURL",     value = var.ai_primary_base_url },
      { name = "AI__PRIMARY__CHATMODEL",   value = var.ai_primary_chat_model },
      # AI 服务 - 备 Provider（硅基流动 DeepSeek）
      { name = "AI__FALLBACK__APIKEY",     value = var.ai_fallback_api_key },
      { name = "AI__FALLBACK__BASEURL",    value = var.ai_fallback_base_url },
      { name = "AI__FALLBACK__CHATMODEL",  value = var.ai_fallback_chat_model },
      # Redis
      { name = "REDIS_CONNECTION",       value = var.redis_connection },
      { name = "JWT__SECRETKEY",          value = var.jwt_secret_key },
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.this.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }

    # Graceful shutdown: give the app time to drain connections
    stopTimeout = 30
  }])

  tags = var.tags
}

# ── ECS Service ───────────────────────────────────────────────────────────────
resource "aws_ecs_service" "this" {
  name            = var.name
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.this.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = var.assign_public_ip
  }

  load_balancer {
    target_group_arn = var.target_group_arn
    container_name   = var.name
    container_port   = var.app_port
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  deployment_controller {
    type = "ECS"
  }

  lifecycle {
    # task_definition is updated by CI/CD on each deploy; ignore Terraform drift
    ignore_changes = [task_definition]
  }

  tags = var.tags
}
