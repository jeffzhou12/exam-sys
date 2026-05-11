output "distribution_id" {
  description = "CloudFront Distribution ID (used for cache invalidation in CI/CD)"
  value       = aws_cloudfront_distribution.frontend.id
}

output "distribution_domain" {
  description = "CloudFront domain name (访问前端的 URL)"
  value       = "https://${aws_cloudfront_distribution.frontend.domain_name}"
}

output "s3_bucket_name" {
  description = "S3 bucket name (used for aws s3 sync in CI/CD)"
  value       = aws_s3_bucket.frontend.bucket
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN (used for IAM policy)"
  value       = aws_s3_bucket.frontend.arn
}
