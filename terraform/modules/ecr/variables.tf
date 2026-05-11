variable "name" {
  description = "ECR repository name"
  type        = string
}

variable "force_delete" {
  description = "Allow deleting non-empty ECR repositories during replacement"
  type        = bool
  default     = false
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
