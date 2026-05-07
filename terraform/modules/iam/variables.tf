variable "name_prefix" {
  description = "Prefix for all IAM resource names"
  type        = string
}

variable "db_secret_arn" {
  description = "ARN of the Secrets Manager secret holding the RDS password"
  type        = string
}

variable "ecr_repository_arn" {
  description = "ARN of the ECR repository that GitHub Actions will push images to"
  type        = string
}

variable "github_repo" {
  description = "GitHub repository in owner/repo format (e.g. acme/go_web)"
  type        = string
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
