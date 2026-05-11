-- =============================================================================
-- ExamSystem 数据库初始化脚本
-- 适用于 PostgreSQL 16+
-- 使用方式：psql -U postgres -d exam_system -f db/init.sql
-- =============================================================================

-- 创建数据库（若在 psql 中执行，需先手动创建数据库）
-- CREATE DATABASE exam_system;

-- 启用必要扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
-- 若要使用向量相似度检测（题目去重），需安装 pgvector 扩展
-- CREATE EXTENSION IF NOT EXISTS "vector";

-- =============================================================================
-- 租户表
-- =============================================================================
CREATE TABLE IF NOT EXISTS tenants (
    id               UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    name             VARCHAR(200) NOT NULL,
    schema_name      VARCHAR(100) NOT NULL UNIQUE,
    contact_email    VARCHAR(320) NOT NULL,
    is_active        BOOLEAN     NOT NULL DEFAULT TRUE,
    ai_call_quota    INTEGER     NOT NULL DEFAULT 1000,
    ai_call_used     INTEGER     NOT NULL DEFAULT 0,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  tenants              IS '租户表，每个租户代表一个独立的考试组织单位';
COMMENT ON COLUMN tenants.schema_name  IS 'PostgreSQL Schema 名称，用于多租户数据隔离';
COMMENT ON COLUMN tenants.ai_call_quota IS '每月 AI 调用配额上限';

-- =============================================================================
-- 题目表
-- =============================================================================
CREATE TABLE IF NOT EXISTS questions (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id        UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    type             SMALLINT     NOT NULL, -- 1=单选 2=多选 3=判断 4=简答
    content          TEXT         NOT NULL,
    options          JSONB,                 -- 选项（单/多选题）
    correct_answer   VARCHAR(2000) NOT NULL,
    explanation      TEXT,
    knowledge_point  VARCHAR(500),
    difficulty       SMALLINT     NOT NULL DEFAULT 1 CHECK (difficulty BETWEEN 1 AND 5),
    is_ai_generated  BOOLEAN      NOT NULL DEFAULT FALSE,
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_questions_tenant_active  ON questions(tenant_id, is_active);
CREATE INDEX IF NOT EXISTS idx_questions_knowledge      ON questions(tenant_id, knowledge_point);
CREATE INDEX IF NOT EXISTS idx_questions_type           ON questions(tenant_id, type);
-- GIN 索引加速 JSONB 检索
CREATE INDEX IF NOT EXISTS idx_questions_options_gin    ON questions USING GIN (options);

COMMENT ON TABLE  questions             IS '题目表，options 使用 JSONB 存储以支持不同题型扩展';
COMMENT ON COLUMN questions.type        IS '1=单选题 2=多选题 3=判断题 4=简答题';
COMMENT ON COLUMN questions.difficulty  IS '难度系数 1-5，1最易 5最难';

-- =============================================================================
-- 试卷表
-- =============================================================================
CREATE TABLE IF NOT EXISTS exam_papers (
    id                   UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id            UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    title                VARCHAR(500) NOT NULL,
    description          TEXT,
    total_score          INTEGER      NOT NULL DEFAULT 100,
    duration_minutes     INTEGER      NOT NULL DEFAULT 90,
    status               SMALLINT     NOT NULL DEFAULT 0, -- 0=草稿 1=已发布 2=进行中 3=已结束 4=已取消
    start_time           TIMESTAMPTZ,
    end_time             TIMESTAMPTZ,
    anti_cheating_enabled BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_exam_papers_tenant_status ON exam_papers(tenant_id, status);

COMMENT ON COLUMN exam_papers.status IS '0=草稿 1=已发布 2=进行中 3=已结束 4=已取消';

-- =============================================================================
-- 试卷题目关联表
-- =============================================================================
CREATE TABLE IF NOT EXISTS exam_questions (
    exam_paper_id UUID     NOT NULL REFERENCES exam_papers(id) ON DELETE CASCADE,
    question_id   UUID     NOT NULL REFERENCES questions(id)   ON DELETE RESTRICT,
    score         INTEGER  NOT NULL DEFAULT 10,
    "order"       INTEGER  NOT NULL DEFAULT 0,
    PRIMARY KEY (exam_paper_id, question_id)
);

-- =============================================================================
-- 考生答题记录表
-- =============================================================================
CREATE TABLE IF NOT EXISTS student_answers (
    id              UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    exam_paper_id   UUID         NOT NULL REFERENCES exam_papers(id) ON DELETE CASCADE,
    question_id     UUID         NOT NULL REFERENCES questions(id)   ON DELETE RESTRICT,
    student_id      VARCHAR(100) NOT NULL,
    answer_content  TEXT         NOT NULL DEFAULT '',
    score           INTEGER,
    grading_status  SMALLINT     NOT NULL DEFAULT 0, -- 0=待评 1=自动 2=AI评 3=人工
    ai_feedback     TEXT,
    submitted_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (exam_paper_id, student_id, question_id)
);

CREATE INDEX IF NOT EXISTS idx_student_answers_exam_student ON student_answers(exam_paper_id, student_id);
CREATE INDEX IF NOT EXISTS idx_student_answers_status       ON student_answers(grading_status) WHERE grading_status = 0;

COMMENT ON COLUMN student_answers.grading_status IS '0=待评分 1=自动评分 2=AI评分 3=人工评分';

-- =============================================================================
-- AI 调用审计日志表
-- =============================================================================
CREATE TABLE IF NOT EXISTS ai_audit_logs (
    id                UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id         UUID         NOT NULL,
    operation         VARCHAR(100) NOT NULL,
    model_name        VARCHAR(100) NOT NULL,
    prompt_tokens     INTEGER      NOT NULL DEFAULT 0,
    completion_tokens INTEGER      NOT NULL DEFAULT 0,
    total_tokens      INTEGER      NOT NULL DEFAULT 0,
    is_success        BOOLEAN      NOT NULL DEFAULT TRUE,
    error_message     TEXT,
    related_entity_id UUID,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_audit_logs_tenant_time ON ai_audit_logs(tenant_id, created_at DESC);

-- =============================================================================
-- 自动更新 updated_at 的触发器
-- =============================================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$ 
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY['tenants','questions','exam_papers','student_answers','ai_audit_logs']
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%s_updated_at
             BEFORE UPDATE ON %s
             FOR EACH ROW EXECUTE FUNCTION update_updated_at_column()',
            t, t
        );
    END LOOP;
EXCEPTION WHEN OTHERS THEN NULL; -- 触发器已存在时忽略
END $$;

-- =============================================================================
-- 插入系统租户（开发/演示用）
-- =============================================================================
INSERT INTO tenants (id, name, schema_name, contact_email, ai_call_quota)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'System Demo Tenant',
    'tenant_system',
    'admin@example.com',
    9999
) ON CONFLICT (id) DO NOTHING;
