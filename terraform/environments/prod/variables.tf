variable "aws_region" {
  description = "AWS region to deploy into"
  type        = string
  default     = "ap-southeast-1"
}

variable "app_name" {
  description = "Application name (used as a resource name prefix)"
  type        = string
  default     = "go-web"
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
