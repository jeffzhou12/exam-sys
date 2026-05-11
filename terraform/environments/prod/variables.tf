variable "aws_region" {
  description = "AWS region to deploy into"
  type        = string
  default     = "ap-southeast-1"
}

variable "app_name" {
  description = "Application name (used as a resource name prefix)"
  type        = string
  default     = "exam"
}

variable "environment" {
  description = "Deployment environment (prod / staging)"
  type        = string
  default     = "prod"
}

variable "github_repo" {
  description = "GitHub repository in owner/repo format"
  type        = string
}

variable "domain_name" {
  description = "Your domain name (e.g. example.com). Leave empty to use HTTP only."
  type        = string
  default     = ""
}

# ── Networking ────────────────────────────────────────────────────────────────
variable "vpc_id" {
  description = "Existing VPC ID (must be the same VPC as the RDS instance)"
  type        = string
}

variable "public_subnet_ids" {
  description = "Public subnet IDs for the ALB (at least 2 AZs)"
  type        = list(string)
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for ECS tasks"
  type        = list(string)
}

variable "assign_public_ip" {
  description = "Set true when ECS tasks run in public subnets (no NAT Gateway)"
  type        = bool
  default     = false
}

# ── RDS ───────────────────────────────────────────────────────────────────────
variable "db_host" {
  description = "RDS instance endpoint"
  type        = string
}

variable "db_user" {
  description = "RDS master username"
  type        = string
  default     = "postgres"
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "postgres"
}

variable "db_secret_arn" {
  description = "Secrets Manager ARN for the RDS password"
  type        = string
}

variable "rds_security_group_id" {
  description = "Security group ID attached to the RDS instance (to add ECS inbound rule)"
  type        = string
}

# ── ECS ───────────────────────────────────────────────────────────────────────
variable "app_port" {
  description = "Container port"
  type        = number
  default     = 8080
}

variable "task_cpu" {
  description = "Fargate task CPU units"
  type        = number
  default     = 256
}

variable "task_memory" {
  description = "Fargate task memory in MiB"
  type        = number
  default     = 512
}

variable "desired_count" {
  description = "Number of running ECS tasks"
  type        = number
  default     = 1
}

variable "ecr_force_delete" {
  description = "Allow deleting non-empty ECR repositories during name replacement"
  type        = bool
  default     = false
}

variable "alb_deletion_protection" {
  description = "Enable ALB deletion protection; keep false during rename/replacement operations"
  type        = bool
  default     = false
}

# ── AI 服务 ───────────────────────────────────────────────────────────────────
variable "ai_api_key" {
  description = "OpenAI / Azure OpenAI API Key"
  type        = string
  sensitive   = true
  default     = ""
}

variable "ai_base_url" {
  description = "AI API base URL"
  type        = string
  default     = "https://api.openai.com/v1"
}

variable "ai_chat_model" {
  description = "Chat model name"
  type        = string
  default     = "gpt-4o"
}

# ── Redis (ElastiCache) ───────────────────────────────────────────────────────
variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_num_clusters" {
  description = "Number of Redis cache nodes (1 = single, 2 = primary + replica)"
  type        = number
  default     = 1
}
