# ─────────────────────────────────────────────────────────────────────────────
# backend.hcl — 独立的 backend 配置，支持多环境复用
# 用法：terraform init -backend-config=backend.hcl
# ─────────────────────────────────────────────────────────────────────────────
bucket       = "go-web-tfstate-183047559773"
key          = "exam-system/prod/terraform.tfstate"
region       = "ap-southeast-1"
encrypt      = true
use_lockfile = true
profile      = "go-web-deploy"
