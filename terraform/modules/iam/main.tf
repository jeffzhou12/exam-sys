# ── ECS Execution Role ────────────────────────────────────────────────────────
# Used by ECS control plane to pull images from ECR and push logs to CloudWatch.
resource "aws_iam_role" "ecs_execution" {
  name = "${var.name_prefix}-ecs-execution"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "ecs_execution_managed" {
  role       = aws_iam_role.ecs_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# ── ECS Task Role ─────────────────────────────────────────────────────────────
# Assumed by the application process itself (fetching secrets, etc.).
resource "aws_iam_role" "ecs_task" {
  name = "${var.name_prefix}-ecs-task"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = var.tags
}

resource "aws_iam_policy" "ecs_task_secrets" {
  name        = "${var.name_prefix}-ecs-task-secrets"
  description = "Allow ECS task to fetch the RDS password from Secrets Manager"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["secretsmanager:GetSecretValue"]
      Resource = var.db_secret_arn
    }]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_secrets" {
  role       = aws_iam_role.ecs_task.name
  policy_arn = aws_iam_policy.ecs_task_secrets.arn
}

# ── GitHub Actions OIDC Provider ──────────────────────────────────────────────
# Only one OIDC provider per URL is allowed per AWS account.
# If it already exists, import it:
#   terraform import aws_iam_openid_connect_provider.github \
#     arn:aws:iam::<ACCOUNT_ID>:oidc-provider/token.actions.githubusercontent.com
resource "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"

  client_id_list = ["sts.amazonaws.com"]

  # GitHub's OIDC thumbprints (intermediate CA SHA-1 fingerprints)
  thumbprint_list = [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1c58a3a8518e8759bf075b76b750d4f2df264fcd",
  ]

  tags = var.tags
}

# ── GitHub Actions Deploy Role ────────────────────────────────────────────────
# Assumed by GitHub Actions via OIDC. Least-privilege: push to ECR + deploy ECS.
resource "aws_iam_role" "github_actions" {
  name = "${var.name_prefix}-github-actions"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        Federated = aws_iam_openid_connect_provider.github.arn
      }
      Action = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringLike = {
          "token.actions.githubusercontent.com:sub" = "repo:${var.github_repo}:*"
        }
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
        }
      }
    }]
  })

  tags = var.tags
}

resource "aws_iam_policy" "github_actions_deploy" {
  name        = "${var.name_prefix}-github-actions-deploy"
  description = "Allow GitHub Actions to push images to ECR and deploy to ECS"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "ECRAuth"
        Effect   = "Allow"
        Action   = ["ecr:GetAuthorizationToken"]
        Resource = "*"
      },
      {
        Sid    = "ECRPush"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage",
          "ecr:PutImage",
          "ecr:InitiateLayerUpload",
          "ecr:UploadLayerPart",
          "ecr:CompleteLayerUpload",
        ]
        Resource = var.ecr_repository_arn
      },
      {
        Sid    = "ECSUpdate"
        Effect = "Allow"
        Action = [
          "ecs:RegisterTaskDefinition",
          "ecs:DescribeTaskDefinition",
          "ecs:UpdateService",
          "ecs:DescribeServices",
        ]
        Resource = "*"
      },
      {
        # Required so ECS can use the task/execution roles when registering a new task def
        Sid    = "IAMPassRole"
        Effect = "Allow"
        Action = ["iam:PassRole"]
        Resource = [
          aws_iam_role.ecs_execution.arn,
          aws_iam_role.ecs_task.arn,
        ]
      },
    ]
  })
}

resource "aws_iam_role_policy_attachment" "github_actions_deploy" {
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.github_actions_deploy.arn
}

# ── S3 + CloudFront 权限（前端部署，可选）────────────────────────────────────
resource "aws_iam_policy" "github_actions_frontend" {
  count       = var.enable_frontend_deploy ? 1 : 0
  name        = "${var.name_prefix}-github-actions-frontend"
  description = "Allow GitHub Actions to deploy frontend to S3 and invalidate CloudFront"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "S3FrontendDeploy"
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket",
        ]
        Resource = [
          var.frontend_bucket_arn,
          "${var.frontend_bucket_arn}/*",
        ]
      },
      {
        Sid      = "CloudFrontInvalidate"
        Effect   = "Allow"
        Action   = ["cloudfront:CreateInvalidation"]
        Resource = ["*"]
      },
    ]
  })
}

resource "aws_iam_role_policy_attachment" "github_actions_frontend" {
  count      = var.enable_frontend_deploy ? 1 : 0
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.github_actions_frontend[0].arn
}
