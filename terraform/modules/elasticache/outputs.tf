output "primary_endpoint" {
  description = "Redis primary endpoint address"
  value       = aws_elasticache_replication_group.this.primary_endpoint_address
}

output "port" {
  description = "Redis port"
  value       = 6379
}

output "connection_string" {
  description = "Redis connection string in host:port format (use as REDIS_CONNECTION env var)"
  value       = "${aws_elasticache_replication_group.this.primary_endpoint_address}:6379"
}

output "security_group_id" {
  description = "Redis security group ID"
  value       = aws_security_group.redis.id
}
