# ── S3 Bucket (前端静态文件) ──────────────────────────────────────────────────
resource "aws_s3_bucket" "frontend" {
  bucket        = var.bucket_name
  force_destroy = var.force_destroy

  tags = var.tags
}

resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  versioning_configuration {
    status = "Enabled"
  }
}

# ── CloudFront Origin Access Control (OAC) ────────────────────────────────────
resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${var.name}-oac"
  description                       = "OAC for ${var.name} frontend S3 bucket"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# ── S3 Bucket Policy (仅允许 CloudFront OAC 读取) ─────────────────────────────
data "aws_iam_policy_document" "s3_cloudfront" {
  statement {
    sid    = "AllowCloudFrontOAC"
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.frontend.arn}/*"]

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.frontend.arn]
    }
  }
}

resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  policy = data.aws_iam_policy_document.s3_cloudfront.json

  depends_on = [aws_s3_bucket_public_access_block.frontend]
}

# ── CloudFront Function: /admin/* SPA 路由重写 ────────────────────────────────
# S3 没有"子目录默认文档"机制，/admin/ 或 /admin/dashboard 等 SPA 路由
# 在 S3 上不存在对应文件，请求会 404。CloudFront Function 在 Viewer Request
# 阶段将这类路径提前重写为 /admin/index.html，避免触发全局 404 回退
# (全局回退只能返回 portal 的 /index.html，导致管理后台无法打开)。
resource "aws_cloudfront_function" "admin_spa_rewrite" {
  name    = "${var.name}-admin-spa-rewrite"
  runtime = "cloudfront-js-2.0"
  publish = true
  comment = "Rewrite /admin/* SPA routes to /admin/index.html"

  code = <<-EOT
    function handler(event) {
      var request = event.request;
      var uri = request.uri;
      // 有文件扩展名（如 .js/.css/.png）的请求直接透传，其余视为 SPA 路由
      if (!uri.match(/\/[^\/]*\.[^\/]+$/)) {
        request.uri = '/admin/index.html';
      }
      return request;
    }
  EOT
}

# ── CloudFront Distribution ───────────────────────────────────────────────────
resource "aws_cloudfront_distribution" "frontend" {
  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"
  price_class         = "PriceClass_All"
  comment             = "${var.name} frontend"

  # 源站 1: S3 (静态文件)
  origin {
    domain_name              = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_id                = "S3-${var.bucket_name}"
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
  }

  # 源站 2: ALB (API 后端)
  origin {
    domain_name = var.alb_dns_name
    origin_id   = "ALB-API"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  # 行为 1: /api/* 转发到 ALB
  ordered_cache_behavior {
    path_pattern           = "/api/*"
    target_origin_id       = "ALB-API"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    # 不缓存 API 响应
    forwarded_values {
      query_string = true
      headers      = ["Authorization", "X-Tenant-ID", "Content-Type", "Accept", "Origin"]
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 0
    max_ttl     = 0
  }

  # 行为 2: /admin/* 走 S3，附加 CF Function 处理 SPA 路由重写
  # 必须放在默认行为之前、/api/* 之后
  ordered_cache_behavior {
    path_pattern           = "/admin/*"
    target_origin_id       = "S3-${var.bucket_name}"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 86400
    max_ttl     = 31536000

    function_association {
      event_type   = "viewer-request"
      function_arn = aws_cloudfront_function.admin_spa_rewrite.arn
    }
  }

  # 默认行为: /* 走 S3 (静态文件，长缓存)
  default_cache_behavior {
    target_origin_id       = "S3-${var.bucket_name}"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 86400    # 1 天
    max_ttl     = 31536000 # 1 年
  }

  # SPA 路由支持：所有 404/403 返回 index.html（让 Vue Router 处理）
  custom_error_response {
    error_code            = 404
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 0
  }

  custom_error_response {
    error_code            = 403
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 0
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = var.tags
}
