# ─────────────────────────────────────────────────────────────────────────────
# Staging 环境配置（复用 prod modules，独立 state）
# ─────────────────────────────────────────────────────────────────────────────
terraform {
  required_version = ">= 1.7"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  # init 时传入 backend.hcl：make init ENV=staging
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

module "ecr" {
  source       = "../../modules/ecr"
  name         = local.name
  force_delete = true # staging 允许强制删除
}

module "iam" {
  source = "../../modules/iam"

  name_prefix            = local.name
  db_secret_arn          = var.db_secret_arn
  ecr_repository_arn     = module.ecr.repository_arn
  github_repo            = var.github_repo
  ecs_task_s3_bucket_arns = [
    "arn:aws:s3:::exam-sys-default",
    "arn:aws:s3:::exam-sys-books",
    "arn:aws:s3:::exam-sys-media",
  ]
  enable_frontend_deploy = true
  frontend_bucket_arn    = module.cloudfront.s3_bucket_arn
  portal_bucket_arn      = module.cloudfront_portal.s3_bucket_arn
}

module "alb" {
  source = "../../modules/alb"

  name              = local.name
  vpc_id            = var.vpc_id
  public_subnet_ids = var.public_subnet_ids
  app_port          = var.app_port
  # staging 不启用删除保护
  enable_deletion_protection = false
  certificate_arn            = ""
}

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

  container_image = "${module.ecr.repository_url}:latest"

  app_port      = var.app_port
  cpu           = 256  # staging 降低规格节省成本
  memory        = 512
  desired_count = 1

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

module "elasticache" {
  source = "../../modules/elasticache"

  name                        = local.name
  vpc_id                      = var.vpc_id
  private_subnet_ids          = var.private_subnet_ids
  ecs_tasks_security_group_id = module.ecs.ecs_tasks_security_group_id
  node_type                   = "cache.t3.micro" # staging 使用最小实例
  num_cache_clusters          = 1
  tags                        = local.common_tags
}

module "cloudfront" {
  source = "../../modules/cloudfront"

  name          = local.name
  bucket_name   = "${local.name}-frontend"
  alb_dns_name  = module.alb.alb_dns_name
  force_destroy = true
  tags          = local.common_tags
}

module "cloudfront_portal" {
  source = "../../modules/cloudfront"

  name          = "${local.name}-portal"
  bucket_name   = "${local.name}-portal"
  alb_dns_name  = module.alb.alb_dns_name
  force_destroy = true
  tags          = local.common_tags
}
