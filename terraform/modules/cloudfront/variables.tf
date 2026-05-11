variable "name" {
  description = "Resource name prefix"
  type        = string
}

variable "bucket_name" {
  description = "S3 bucket name for frontend static files"
  type        = string
}

variable "alb_dns_name" {
  description = "ALB DNS name (API backend origin)"
  type        = string
}

variable "force_destroy" {
  description = "Allow bucket destruction even when non-empty"
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
