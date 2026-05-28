-- Migration: 20260530_user_favorites
-- 用户收藏表（题目 / 试卷 / 图书）

CREATE TABLE IF NOT EXISTS user_favorites (
    id          UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id   UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     VARCHAR(100) NOT NULL,
    target_type SMALLINT    NOT NULL,   -- 1=题目 2=试卷 3=图书
    target_id   UUID        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_favorites UNIQUE (tenant_id, user_id, target_type, target_id)
);

CREATE INDEX IF NOT EXISTS idx_user_favorites_user
    ON user_favorites (tenant_id, user_id, target_type);
