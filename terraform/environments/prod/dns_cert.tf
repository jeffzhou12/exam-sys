# ── Route 53 Hosted Zone ──────────────────────────────────────────────────────
# References an existing Route 53 hosted zone for your domain.
# After registering your domain, a hosted zone is created automatically in Route 53.
data "aws_route53_zone" "this" {
  count = var.domain_name != "" ? 1 : 0
  name  = var.domain_name
}

# ── ACM Certificate ───────────────────────────────────────────────────────────
resource "aws_acm_certificate" "this" {
  count = var.domain_name != "" ? 1 : 0

  domain_name               = var.domain_name
  subject_alternative_names = ["www.${var.domain_name}"]
  validation_method         = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = local.common_tags
}

# ── DNS validation records ────────────────────────────────────────────────────
resource "aws_route53_record" "cert_validation" {
  for_each = var.domain_name != "" ? {
    for dvo in aws_acm_certificate.this[0].domain_validation_options :
    dvo.domain_name => {
      name   = dvo.resource_record_name
      type   = dvo.resource_record_type
      record = dvo.resource_record_value
    }
  } : {}

  zone_id = data.aws_route53_zone.this[0].zone_id
  name    = each.value.name
  type    = each.value.type
  records = [each.value.record]
  ttl     = 60
}

resource "aws_acm_certificate_validation" "this" {
  count = var.domain_name != "" ? 1 : 0

  certificate_arn         = aws_acm_certificate.this[0].arn
  validation_record_fqdns = [for r in aws_route53_record.cert_validation : r.fqdn]
}

# ── A Record pointing to ALB ──────────────────────────────────────────────────
resource "aws_route53_record" "app" {
  count = var.domain_name != "" ? 1 : 0

  zone_id = data.aws_route53_zone.this[0].zone_id
  name    = var.domain_name
  type    = "A"

  alias {
    name                   = module.alb.alb_dns_name
    zone_id                = data.aws_lb.this[0].zone_id
    evaluate_target_health = true
  }
}

data "aws_lb" "this" {
  count = var.domain_name != "" ? 1 : 0
  arn   = module.alb.alb_arn
}
