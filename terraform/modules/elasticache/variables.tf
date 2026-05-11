variable "name" {
  description = "Name prefix for all ElastiCache resources"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID for the Redis security group"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for the ElastiCache subnet group (at least 2 AZs)"
  type        = list(string)
}

variable "ecs_tasks_security_group_id" {
  description = "ECS tasks security group ID — only this SG may reach Redis on port 6379"
  type        = string
}

variable "node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "num_cache_clusters" {
  description = "Number of cache nodes (1 = single node, 2 = primary + replica)"
  type        = number
  default     = 1
}

variable "snapshot_retention_days" {
  description = "Number of days to retain automatic snapshots (0 = disabled)"
  type        = number
  default     = 1
}

variable "tags" {
  description = "Additional tags to apply to all resources"
  type        = map(string)
  default     = {}
}
