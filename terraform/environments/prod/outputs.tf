output "alb_dns_name" {
  description = "Access the application at this URL"
  value       = var.domain_name != "" ? "https://${var.domain_name}" : "http://${module.alb.alb_dns_name}"
}

output "ecr_repository_url" {
  description = "ECR repository URL (used in docker push commands)"
  value       = module.ecr.repository_url
}

output "ecs_cluster_name" {
  description = "ECS cluster name (used in GitHub Actions)"
  value       = module.ecs.cluster_name
}

output "ecs_service_name" {
  description = "ECS service name (used in GitHub Actions)"
  value       = module.ecs.service_name
}

output "github_actions_role_arn" {
  description = "Set this as GitHub secret AWS_DEPLOY_ROLE_ARN"
  value       = module.iam.github_actions_role_arn
}

output "ecs_tasks_security_group_id" {
  description = "ECS tasks security group ID (already added to RDS SG via Terraform)"
  value       = module.ecs.ecs_tasks_security_group_id
}

output "redis_primary_endpoint" {
  description = "ElastiCache Redis primary endpoint"
  value       = module.elasticache.primary_endpoint
}

output "redis_connection_string" {
  description = "Redis connection string injected into ECS tasks as REDIS_CONNECTION"
  value       = module.elasticache.connection_string
}

output "frontend_url" {
  description = "Admin 管理后台访问地址（CloudFront）"
  value       = module.cloudfront.distribution_domain
}

output "cloudfront_distribution_id" {
  description = "Admin CloudFront Distribution ID（CI/CD 缓存失效用）"
  value       = module.cloudfront.distribution_id
}

output "frontend_s3_bucket" {
  description = "Admin 前端静态文件 S3 Bucket 名称（CI/CD 上传用）"
  value       = module.cloudfront.s3_bucket_name
}

output "portal_url" {
  description = "Portal 前台考生端访问地址（CloudFront）"
  value       = module.cloudfront_portal.distribution_domain
}

output "portal_cloudfront_distribution_id" {
  description = "Portal CloudFront Distribution ID（CI/CD 缓存失效用）"
  value       = module.cloudfront_portal.distribution_id
}

output "portal_s3_bucket" {
  description = "Portal 前端静态文件 S3 Bucket 名称（CI/CD 上传用）"
  value       = module.cloudfront_portal.s3_bucket_name
}
