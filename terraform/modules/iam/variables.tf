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

variable "enable_frontend_deploy" {
  description = "Whether to create the S3+CloudFront deploy policy for GitHub Actions"
  type        = bool
  default     = false
}

variable "frontend_bucket_arn" {
  description = "ARN of the S3 bucket for frontend static files"
  type        = string
  default     = ""
}

variable "cloudfront_distribution_arn" {
  description = "ARN of the CloudFront distribution (for cache invalidation)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
