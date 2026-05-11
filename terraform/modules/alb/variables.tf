variable "name" {
  description = "Name prefix for ALB resources"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID where the ALB is created"
  type        = string
}

variable "public_subnet_ids" {
  description = "List of public subnet IDs for the ALB (must span at least 2 AZs)"
  type        = list(string)
}

variable "app_port" {
  description = "Port the application container listens on"
  type        = number
  default     = 8080
}

variable "certificate_arn" {
  description = "ACM certificate ARN for HTTPS. Leave empty to use HTTP only."
  type        = string
  default     = ""
}

variable "enable_deletion_protection" {
  description = "Enable ALB deletion protection"
  type        = bool
  default     = true
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
