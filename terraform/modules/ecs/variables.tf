variable "name" {
  description = "Name for ECS cluster, service, and task definition family"
  type        = string
}

variable "aws_region" {
  description = "AWS region (used for CloudWatch log driver)"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID for ECS tasks security group"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for ECS tasks (same VPC as RDS)"
  type        = list(string)
}

variable "alb_security_group_id" {
  description = "Security group ID of the ALB (to allow inbound to ECS tasks)"
  type        = string
}

variable "target_group_arn" {
  description = "ALB target group ARN to register ECS tasks"
  type        = string
}

variable "execution_role_arn" {
  description = "ARN of the ECS task execution role"
  type        = string
}

variable "task_role_arn" {
  description = "ARN of the ECS task role (application permissions)"
  type        = string
}

variable "container_image" {
  description = "Initial container image (e.g. <account>.dkr.ecr.<region>.amazonaws.com/<repo>:latest)"
  type        = string
}

variable "app_port" {
  description = "Port the container listens on"
  type        = number
  default     = 8080
}

variable "cpu" {
  description = "Fargate task CPU units (256 = 0.25 vCPU)"
  type        = number
  default     = 256
}

variable "memory" {
  description = "Fargate task memory in MiB"
  type        = number
  default     = 512
}

variable "desired_count" {
  description = "Desired number of ECS task instances"
  type        = number
  default     = 1
}

variable "db_host" {
  description = "RDS endpoint hostname"
  type        = string
}

variable "db_user" {
  description = "RDS database user"
  type        = string
  default     = "postgres"
}

variable "db_name" {
  description = "RDS database name"
  type        = string
  default     = "postgres"
}

variable "db_secret_arn" {
  description = "Secrets Manager ARN for the RDS password"
  type        = string
}

variable "assign_public_ip" {
  description = "Assign public IP to Fargate tasks (set true when using public subnets without NAT)"
  type        = bool
  default     = false
}

# ── AI 服务（主 Provider: DeepSeek 官方，备 Provider: 硅基流动）────────────────
variable "ai_primary_api_key" {
  description = "DeepSeek 官方 API Key (platform.deepseek.com)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "ai_primary_base_url" {
  description = "DeepSeek 官方 API Base URL"
  type        = string
  default     = "https://api.deepseek.com/v1"
}

variable "ai_primary_chat_model" {
  description = "DeepSeek 对话模型名称"
  type        = string
  default     = "deepseek-chat"
}

variable "ai_fallback_api_key" {
  description = "硅基流动备用 API Key (cloud.siliconflow.cn)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "ai_fallback_base_url" {
  description = "硅基流动 API Base URL"
  type        = string
  default     = "https://api.siliconflow.cn/v1"
}

variable "ai_fallback_chat_model" {
  description = "硅基流动 DeepSeek 模型名称"
  type        = string
  default     = "deepseek-ai/DeepSeek-V3"
}

# ── Redis ─────────────────────────────────────────────────────────────────────
variable "redis_connection" {
  description = "Redis connection string (host:port)"
  type        = string
  default     = ""
}

# ── JWT ───────────────────────────────────────────────────────────────────────
variable "jwt_secret_key" {
  description = "JWT signing secret key (min 32 chars)"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
