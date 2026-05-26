CREATE TABLE IF NOT EXISTS sms_templates (
    id UUID PRIMARY KEY,
    tenant_id UUID NULL REFERENCES tenants(id) ON DELETE CASCADE,
    scene VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    template_body TEXT NOT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    priority INTEGER NOT NULL DEFAULT 0,
    description VARCHAR(500) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_sms_templates_tenant_scene_enabled
    ON sms_templates(tenant_id, scene, is_enabled);

COMMENT ON TABLE sms_templates IS '短信模板配置，支持系统级和租户级模板';
COMMENT ON COLUMN sms_templates.template_body IS '支持占位符：{code}、{scene}、{target}、{appName}';