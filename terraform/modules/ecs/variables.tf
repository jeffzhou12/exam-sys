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

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
