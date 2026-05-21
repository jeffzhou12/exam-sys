ENV         ?= prod
TF_DIR      := terraform/environments/$(ENV)
BACKEND_CFG := $(TF_DIR)/backend.hcl
VAR_FILE    := $(TF_DIR)/terraform.tfvars

.PHONY: init plan apply destroy fmt validate

## terraform init（使用独立 backend 配置）
init:
	terraform -chdir=$(TF_DIR) init -backend-config=../../$(BACKEND_CFG) -reconfigure

## 格式化所有 .tf 文件
fmt:
	terraform -chdir=terraform fmt -recursive

## 验证语法
validate: init
	terraform -chdir=$(TF_DIR) validate

## 生成执行计划（不执行）
plan: init
	terraform -chdir=$(TF_DIR) plan -var-file=terraform.tfvars -out=tfplan

## 应用变更（需先 plan）
apply:
	terraform -chdir=$(TF_DIR) apply tfplan

## 强制刷新计划并立即应用（适合 CI）
apply-auto: init
	terraform -chdir=$(TF_DIR) apply -var-file=terraform.tfvars -auto-approve

## 销毁（危险操作，需二次确认）
destroy:
	@echo "⚠️  即将销毁 $(ENV) 环境所有资源，请输入环境名称确认："
	@read -p "输入 '$(ENV)' 继续: " CONFIRM; \
	[ "$$CONFIRM" = "$(ENV)" ] && \
	  terraform -chdir=$(TF_DIR) destroy -var-file=terraform.tfvars || \
	  echo "已取消"

## 查看当前状态
state:
	terraform -chdir=$(TF_DIR) show

## 切换到 staging：make plan ENV=staging
## 切换到 prod：   make plan ENV=prod
