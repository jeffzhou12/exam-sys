terraform {
  required_version = ">= 1.7"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # backend 配置已提取到 backend.hcl，支持多环境复用：
  #   terraform init -backend-config=backend.hcl
  backend "s3" {}
}

provider "aws" {
  region  = var.aws_region
  profile = "go-web-deploy"

  default_tags {
    tags = local.common_tags
  }
}

locals {
  name        = "${var.app_name}-${var.environment}"
  common_tags = {
    Project     = var.app_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── ECR ───────────────────────────────────────────────────────────────────────
module "ecr" {
  source = "../../modules/ecr"
  name         = local.name
  force_delete = var.ecr_force_delete
}

# ── IAM ───────────────────────────────────────────────────────────────────────
module "iam" {
  source = "../../modules/iam"

  name_prefix         = local.name
  db_secret_arn       = var.db_secret_arn
  ecr_repository_arn  = module.ecr.repository_arn
  github_repo         = var.github_repo
  ecs_task_s3_bucket_arns = [
    "arn:aws:s3:::exam-sys-default",
    "arn:aws:s3:::exam-sys-books",
    "arn:aws:s3:::exam-sys-media",
  ]
  enable_frontend_deploy = true
  frontend_bucket_arn    = module.cloudfront.s3_bucket_arn
  portal_bucket_arn      = module.cloudfront_portal.s3_bucket_arn
}

# ── ALB ───────────────────────────────────────────────────────────────────────
module "alb" {
  source = "../../modules/alb"

  name                       = local.name
  vpc_id                     = var.vpc_id
  public_subnet_ids          = var.public_subnet_ids
  app_port                   = var.app_port
  enable_deletion_protection = var.alb_deletion_protection
  certificate_arn            = var.domain_name != "" ? aws_acm_certificate_validation.this[0].certificate_arn : ""
}

# ── ECS ───────────────────────────────────────────────────────────────────────
module "ecs" {
  source = "../../modules/ecs"

  name               = local.name
  aws_region         = var.aws_region
  vpc_id             = var.vpc_id
  private_subnet_ids = var.private_subnet_ids

  alb_security_group_id = module.alb.security_group_id
  target_group_arn      = module.alb.target_group_arn
  execution_role_arn    = module.iam.ecs_execution_role_arn
  task_role_arn         = module.iam.ecs_task_role_arn

  # Placeholder image — CI/CD will update this on first deploy
  container_image = "${module.ecr.repository_url}:latest"

  app_port      = var.app_port
  cpu           = var.task_cpu
  memory        = var.task_memory
  desired_count = var.desired_count

  assign_public_ip = var.assign_public_ip

  db_host       = var.db_host
  db_user       = var.db_user
  db_name       = var.db_name
  db_secret_arn = var.db_secret_arn

  ai_primary_api_key     = var.ai_primary_api_key
  ai_primary_base_url    = var.ai_primary_base_url
  ai_primary_chat_model  = var.ai_primary_chat_model
  ai_fallback_api_key    = var.ai_fallback_api_key
  ai_fallback_base_url   = var.ai_fallback_base_url
  ai_fallback_chat_model = var.ai_fallback_chat_model

  jwt_secret_key   = var.jwt_secret_key
  redis_connection = module.elasticache.connection_string
}

# ── ElastiCache (Redis) ───────────────────────────────────────────────────────
module "elasticache" {
  source = "../../modules/elasticache"

  name                        = local.name
  vpc_id                      = var.vpc_id
  private_subnet_ids          = var.private_subnet_ids
  ecs_tasks_security_group_id = module.ecs.ecs_tasks_security_group_id
  node_type                   = var.redis_node_type
  num_cache_clusters          = var.redis_num_clusters
  tags                        = local.common_tags
}

# ── RDS Security Group Rule ────────────────────────────────────────────────────
# Allow ECS tasks to reach RDS on port 5432.
# The RDS instance was created outside this Terraform state, so we add the rule
# by referencing the existing security group ID.
resource "aws_security_group_rule" "rds_from_ecs" {
  type                     = "ingress"
  description              = "Allow ECS tasks to connect to RDS"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  security_group_id        = var.rds_security_group_id
  source_security_group_id = module.ecs.ecs_tasks_security_group_id
}

# ── CloudFront + S3 (前端静态资源) ────────────────────────────────────────────
module "cloudfront" {
  source = "../../modules/cloudfront"

  name          = local.name
  bucket_name   = "${local.name}-frontend"
  alb_dns_name  = module.alb.alb_dns_name
  force_destroy = var.ecr_force_delete # 开发环境允许销毁，生产建议设 false
  tags          = local.common_tags
}

# ── CloudFront + S3 (Portal 前台考生端) ──────────────────────────────────────
module "cloudfront_portal" {
  source = "../../modules/cloudfront"

  name          = "${local.name}-portal"
  bucket_name   = "${local.name}-portal"
  alb_dns_name  = module.alb.alb_dns_name
  force_destroy = var.ecr_force_delete
  tags          = local.common_tags
}
