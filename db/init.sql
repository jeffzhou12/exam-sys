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
CREATE EXTENSION IF NOT EXISTS "vector";

-- =============================================================================
-- 清理已存在的对象（保证脚本可重复执行）
-- 按外键依赖逆序删除，CASCADE 同时清除关联的索引、约束、触发器
-- =============================================================================
DROP TABLE IF EXISTS book_annotations  CASCADE;
DROP TABLE IF EXISTS books             CASCADE;
DROP TABLE IF EXISTS practice_sessions CASCADE;
DROP TABLE IF EXISTS messages          CASCADE;
DROP TABLE IF EXISTS student_answers   CASCADE;
DROP TABLE IF EXISTS exam_questions    CASCADE;
DROP TABLE IF EXISTS audit_logs        CASCADE;
DROP TABLE IF EXISTS ai_audit_logs     CASCADE;
DROP TABLE IF EXISTS ai_model_configs  CASCADE;
DROP TABLE IF EXISTS exam_papers       CASCADE;
DROP TABLE IF EXISTS questions         CASCADE;
DROP TABLE IF EXISTS users             CASCADE;
DROP TABLE IF EXISTS tenants           CASCADE;

DROP FUNCTION IF EXISTS update_updated_at_column() CASCADE;

-- =============================================================================
-- 租户表
-- =============================================================================
CREATE TABLE tenants (
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
CREATE TABLE questions (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id        UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    type             SMALLINT     NOT NULL, -- 1=单选 2=多选 3=判断 4=简答
    content          VARCHAR(4000) NOT NULL,
    options          JSONB,                 -- 选项（单/多选题）
    correct_answer   VARCHAR(2000) NOT NULL,
    explanation      VARCHAR(2000),
    knowledge_point  VARCHAR(500),
    difficulty       SMALLINT     NOT NULL DEFAULT 1 CHECK (difficulty BETWEEN 1 AND 5),
    is_ai_generated  BOOLEAN      NOT NULL DEFAULT FALSE,
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    embedding        real[],                -- 向量嵌入，用于题目去重检测（float[]，对应 C# float[]）
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
CREATE TABLE exam_papers (
    id                   UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id            UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    title                VARCHAR(500) NOT NULL,
    description          VARCHAR(2000),
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
CREATE TABLE exam_questions (
    exam_paper_id UUID     NOT NULL REFERENCES exam_papers(id) ON DELETE CASCADE,
    question_id   UUID     NOT NULL REFERENCES questions(id)   ON DELETE RESTRICT,
    score         INTEGER  NOT NULL DEFAULT 10,
    "order"       INTEGER  NOT NULL DEFAULT 0,
    PRIMARY KEY (exam_paper_id, question_id)
);

-- =============================================================================
-- 考生答题记录表
-- =============================================================================
CREATE TABLE student_answers (
    id              UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    exam_paper_id   UUID         NOT NULL REFERENCES exam_papers(id) ON DELETE CASCADE,
    question_id     UUID         NOT NULL REFERENCES questions(id)   ON DELETE RESTRICT,
    student_id      VARCHAR(100) NOT NULL,
    answer_content  VARCHAR(8000) NOT NULL DEFAULT '',
    score           INTEGER,
    grading_status  SMALLINT     NOT NULL DEFAULT 0, -- 0=待评 1=自动 2=AI评 3=人工
    ai_feedback     VARCHAR(4000),
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
CREATE TABLE ai_audit_logs (
    id                UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id         UUID         NOT NULL,
    operation         VARCHAR(100) NOT NULL,
    model_name        VARCHAR(100) NOT NULL,
    prompt_tokens     INTEGER      NOT NULL DEFAULT 0,
    completion_tokens INTEGER      NOT NULL DEFAULT 0,
    total_tokens      INTEGER      NOT NULL DEFAULT 0,
    is_success        BOOLEAN      NOT NULL DEFAULT TRUE,
    error_message     VARCHAR(1000),
    related_entity_id UUID,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_audit_logs_tenant_time ON ai_audit_logs(tenant_id, created_at DESC);

-- =============================================================================
-- 全局审计日志表
-- 记录所有业务操作的完整轨迹（由 RequestAuditMiddleware 写入）
-- =============================================================================
CREATE TABLE audit_logs (
    id              UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id       UUID,                        -- NULL 表示系统级操作（如超级管理员）
    user_id         UUID,                        -- NULL 表示匿名/未认证请求
    username        VARCHAR(100),
    role            VARCHAR(50),
    action          VARCHAR(20)  NOT NULL,        -- HTTP 方法：GET POST PUT PATCH DELETE
    entity_type     VARCHAR(100),                -- 受影响的资源类型，如 Question ExamPaper User
    entity_id       VARCHAR(100),                -- 受影响的资源 ID（字符串，兼容 UUID 和其他格式）
    request_path    VARCHAR(500) NOT NULL,
    query_string    VARCHAR(2000),
    status_code     SMALLINT     NOT NULL,
    duration_ms     INTEGER      NOT NULL DEFAULT 0,
    ip_address      VARCHAR(45),                 -- 支持 IPv4 / IPv6
    user_agent      VARCHAR(500),
    old_values      JSONB,                       -- 变更前的字段快照（UPDATE/DELETE 时由应用层填充）
    new_values      JSONB,                       -- 变更后的字段快照（CREATE/UPDATE 时由应用层填充）
    error_message   VARCHAR(2000),               -- 请求失败时的错误摘要
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_logs_tenant_time  ON audit_logs(tenant_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_time    ON audit_logs(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity       ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_status       ON audit_logs(status_code) WHERE status_code >= 400;

COMMENT ON TABLE  audit_logs              IS '全局审计日志表，记录所有 API 请求的操作轨迹';
COMMENT ON COLUMN audit_logs.entity_type  IS '受影响的资源类型，如 Question / ExamPaper / User';
COMMENT ON COLUMN audit_logs.old_values   IS 'UPDATE/DELETE 前的字段快照，由应用层手动填充';
COMMENT ON COLUMN audit_logs.new_values   IS 'CREATE/UPDATE 后的字段快照，由应用层手动填充';

-- =============================================================================
-- 用户账号表
-- =============================================================================
CREATE TABLE users (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id        UUID         REFERENCES tenants(id) ON DELETE CASCADE,   -- NULL 表示系统级管理员
    username         VARCHAR(100) NOT NULL,
    password_hash    VARCHAR(512) NOT NULL,
    email            VARCHAR(320),
    role             VARCHAR(50)  NOT NULL DEFAULT 'Student', -- Admin | Teacher | Student
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    last_login_at    TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    -- 同一租户内用户名唯一；系统管理员（tenant_id IS NULL）间用户名也唯一
    UNIQUE (tenant_id, username)
);

CREATE INDEX IF NOT EXISTS idx_users_tenant_active ON users(tenant_id, is_active);
CREATE INDEX IF NOT EXISTS idx_users_role          ON users(tenant_id, role);

COMMENT ON TABLE  users            IS '用户账号表，支持系统级管理员（tenant_id=NULL）和租户级用户';
COMMENT ON COLUMN users.tenant_id  IS 'NULL=系统管理员；非 NULL=所属租户的普通用户';
COMMENT ON COLUMN users.role       IS 'Admin=管理员 Teacher=教师 Student=学生';

-- =============================================================================
-- 站内信表
-- =============================================================================
CREATE TABLE messages (
    id                    UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id             UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    sender_id             UUID         NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    sender_name           VARCHAR(100) NOT NULL,
    recipient_id          UUID         NOT NULL REFERENCES users(id)   ON DELETE CASCADE,
    recipient_name        VARCHAR(100) NOT NULL,
    subject               VARCHAR(500) NOT NULL,
    body                  VARCHAR(4000) NOT NULL,
    attached_question_ids JSONB,                -- 题目 UUID 数组，如 ["uuid1","uuid2"]
    attached_exam_paper_id UUID,
    is_read               BOOLEAN     NOT NULL DEFAULT FALSE,
    parent_message_id     UUID        REFERENCES messages(id) ON DELETE SET NULL,  -- 非空表示这是回复消息
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_messages_recipient ON messages(recipient_id, is_read);
CREATE INDEX IF NOT EXISTS idx_messages_sender    ON messages(sender_id);
CREATE INDEX IF NOT EXISTS idx_messages_tenant    ON messages(tenant_id, created_at DESC);

COMMENT ON TABLE  messages                    IS '站内信表，支持学生向教师发送求助消息并附带题目';
COMMENT ON COLUMN messages.attached_question_ids IS 'JSONB 数组，存储附带的题目 UUID 列表';

-- =============================================================================
-- 图书表
-- =============================================================================
CREATE TABLE books (
    id                  UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id           UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    title               VARCHAR(500) NOT NULL,
    author              VARCHAR(300),
    publisher           VARCHAR(300),
    description         VARCHAR(2000),
    cover_image_url     VARCHAR(1000),
    pdf_file_path       VARCHAR(500),            -- 上传文件在服务器的相对路径
    category            VARCHAR(100),
    tags                JSONB,                   -- 标签数组，如 ["计算机","网络"]
    publish_year        SMALLINT,
    isbn                VARCHAR(30),
    page_count          INTEGER      NOT NULL DEFAULT 0,
    file_size_bytes     BIGINT       NOT NULL DEFAULT 0,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    uploaded_by_id      UUID         NOT NULL,
    uploaded_by_name    VARCHAR(100) NOT NULL DEFAULT '',
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_books_tenant_active   ON books(tenant_id, is_active);
CREATE INDEX IF NOT EXISTS idx_books_category        ON books(tenant_id, category);
CREATE INDEX IF NOT EXISTS idx_books_title           ON books USING GIN (to_tsvector('simple', title));
CREATE INDEX IF NOT EXISTS idx_books_tags_gin        ON books USING GIN (tags);

COMMENT ON TABLE  books               IS '图书资源表，支持 PDF 上传与多维属性筛选';
COMMENT ON COLUMN books.tags          IS 'JSONB 数组，存储多个标签字符串';
COMMENT ON COLUMN books.pdf_file_path IS '服务器端相对存储路径，通过 /api/books/{id}/pdf 接口流式输出';

-- =============================================================================
-- 图书标注表（书签 / 阅读备注 / AI 问答）
-- =============================================================================
CREATE TABLE book_annotations (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    book_id          UUID         NOT NULL REFERENCES books(id) ON DELETE CASCADE,
    user_id          UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    user_name        VARCHAR(100) NOT NULL DEFAULT '',
    page_number      INTEGER       NOT NULL DEFAULT 1,
    selected_text    VARCHAR(2000),               -- 用户框选的原文片段
    note             VARCHAR(2000),               -- 手写备注
    annotation_type  SMALLINT      NOT NULL DEFAULT 1, -- 1=书签 2=阅读备注 3=AI问答
    ai_question      VARCHAR(1000),               -- AI 提问内容
    ai_answer        VARCHAR(8000),               -- AI 回答
    position_json    JSONB,                       -- 位置信息 { x, y, width, height }（0~1 比例）
    highlight_color  VARCHAR(20)   NOT NULL DEFAULT '#FFEB3B',
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_book_ann_book_user  ON book_annotations(book_id, user_id);
CREATE INDEX IF NOT EXISTS idx_book_ann_user       ON book_annotations(user_id, annotation_type);

COMMENT ON TABLE  book_annotations               IS '图书标注表，每条记录对应一次书签、备注或 AI 问答';
COMMENT ON COLUMN book_annotations.annotation_type IS '1=书签  2=阅读备注  3=AI问答';

-- =============================================================================
-- 在线练习会话记录表
-- 存储每次在线练习的成绩摘要，支持跨设备恢复历史记录
-- =============================================================================
CREATE TABLE practice_sessions (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id        UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    student_id       VARCHAR(100) NOT NULL,
    count            INTEGER      NOT NULL,
    correct_count    INTEGER      NOT NULL DEFAULT 0,
    total_score      INTEGER      NOT NULL DEFAULT 0,
    max_score        INTEGER      NOT NULL DEFAULT 0,
    type_name        VARCHAR(50),
    knowledge_point  VARCHAR(200),
    question_type    INTEGER,
    difficulty       INTEGER,
    setup_count      INTEGER      NOT NULL DEFAULT 10,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_practice_sessions_student ON practice_sessions(tenant_id, student_id, created_at DESC);

COMMENT ON TABLE practice_sessions IS '在线练习会话记录，持久化保存每次练习成绩，支持跨设备恢复';

-- =============================================================================
-- AI 模型配置表
-- 支持超级管理员配置系统级默认模型，以及租户管理员配置本租户专属模型
-- scene 枚举值（VARCHAR）：Default GenerateQuestions GradeAnswer ExplainQuestion AnalyzeBook Embedding
-- =============================================================================
CREATE TABLE ai_model_configs (
    id                        UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id                 UUID         REFERENCES tenants(id) ON DELETE CASCADE,  -- NULL = 系统级配置
    scene                     VARCHAR(64)  NOT NULL DEFAULT 'Default',                -- AiScene 枚举名称
    provider_name             VARCHAR(128) NOT NULL,
    base_url                  VARCHAR(512) NOT NULL,
    api_key                   VARCHAR(512) NOT NULL,
    chat_model                VARCHAR(256) NOT NULL,
    embedding_model           VARCHAR(256),
    max_tokens                INTEGER      NOT NULL DEFAULT 4096,
    temperature               DOUBLE PRECISION NOT NULL DEFAULT 0.7,
    monthly_quota_tokens      BIGINT,                                                 -- NULL = 不限额
    used_tokens_current_month BIGINT       NOT NULL DEFAULT 0,
    quota_reset_at            TIMESTAMPTZ,
    is_enabled                BOOLEAN      NOT NULL DEFAULT TRUE,
    priority                  INTEGER      NOT NULL DEFAULT 0,
    description               VARCHAR(500),
    created_at                TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at                TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_model_configs_tenant_scene
    ON ai_model_configs(tenant_id, scene, is_enabled);

COMMENT ON TABLE  ai_model_configs                     IS 'AI 模型配置表，支持系统级与租户级按场景细粒度配置';
COMMENT ON COLUMN ai_model_configs.tenant_id           IS 'NULL=系统级配置；非 NULL=租户专属配置，优先于系统级';
COMMENT ON COLUMN ai_model_configs.scene               IS 'AiScene 枚举：Default/GenerateQuestions/GradeAnswer/ExplainQuestion/AnalyzeBook/Embedding';
COMMENT ON COLUMN ai_model_configs.monthly_quota_tokens IS 'NULL 表示无限额；超出配额后该配置停止参与解析';
COMMENT ON COLUMN ai_model_configs.priority            IS '同一 tenant+scene 下多条配置时，priority 越大越优先';

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
    FOREACH t IN ARRAY ARRAY['tenants','users','questions','exam_papers','student_answers','ai_audit_logs','ai_model_configs','messages','books','book_annotations']
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%s_updated_at
             BEFORE UPDATE ON %s
             FOR EACH ROW EXECUTE FUNCTION update_updated_at_column()',
            t, t
        );
    END LOOP;
END $$;

-- =============================================================================
-- Demo 数据：租户
-- =============================================================================
INSERT INTO tenants (id, name, schema_name, contact_email, ai_call_quota)
VALUES
    (
        '00000000-0000-0000-0000-000000000001',
        '示范学院',
        'tenant_demo',
        'admin@demo-college.edu',
        9999
    ),
    (
        '00000000-0000-0000-0000-000000000002',
        '测试高中',
        'tenant_test',
        'it@test-highschool.edu',
        500
    )
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- Demo 数据：用户（密码均为 Admin@123456，使用 ASP.NET Core Identity PasswordHasher 生成）
-- 如需重新生成哈希，运行: new PasswordHasher<string>().HashPassword(username, password)
-- =============================================================================
INSERT INTO users (id, tenant_id, username, password_hash, email, role)
VALUES
    -- 系统级超级管理员（tenant_id = NULL）
    (
        'a0000000-0000-0000-0000-000000000001',
        NULL,
        'admin',
        'AQAAAAIAAYagAAAAEG2C9qaHjYF5FyBeBUmwOAfTOsB9yF2e7mOEFtzt4gbP5XlHkMa3G4JN/N6RSiSpzg==',
        'admin@system.local',
        'SuperAdmin'
    ),
    -- 示范学院 管理员
    (
        'a0000000-0000-0000-0000-000000000002',
        '00000000-0000-0000-0000-000000000001',
        'demo_admin',
        'AQAAAAIAAYagAAAAEG2C9qaHjYF5FyBeBUmwOAfTOsB9yF2e7mOEFtzt4gbP5XlHkMa3G4JN/N6RSiSpzg==',
        'admin@demo-college.edu',
        'Admin'
    ),
    -- 示范学院 教师
    (
        'a0000000-0000-0000-0000-000000000003',
        '00000000-0000-0000-0000-000000000001',
        'teacher01',
        'AQAAAAIAAYagAAAAEG2C9qaHjYF5FyBeBUmwOAfTOsB9yF2e7mOEFtzt4gbP5XlHkMa3G4JN/N6RSiSpzg==',
        'teacher01@demo-college.edu',
        'Teacher'
    ),
    -- 示范学院 学生
    (
        'a0000000-0000-0000-0000-000000000004',
        '00000000-0000-0000-0000-000000000001',
        'student001',
        'AQAAAAIAAYagAAAAEG2C9qaHjYF5FyBeBUmwOAfTOsB9yF2e7mOEFtzt4gbP5XlHkMa3G4JN/N6RSiSpzg==',
        'student001@demo-college.edu',
        'Student'
    )
ON CONFLICT (tenant_id, username) DO NOTHING;

-- =============================================================================
-- Demo 数据：题目（tenant_id = 示范学院，主题：计算机基础）
-- =============================================================================

-- ---------- 单选题（type = 1） ----------
INSERT INTO questions (id, tenant_id, type, content, options, correct_answer, explanation, knowledge_point, difficulty)
VALUES
    (
        '10000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        1,
        'HTTP 协议默认使用的端口号是？',
        '{"A":"80","B":"8080","C":"443","D":"3000"}',
        'A',
        'HTTP 默认端口为 80，HTTPS 默认端口为 443，8080 通常作为开发/代理端口使用。',
        'HTTP协议',
        1
    ),
    (
        '10000000-0000-0000-0000-000000000002',
        '00000000-0000-0000-0000-000000000001',
        1,
        '以下排序算法中，平均时间复杂度为 O(n log n) 的是？',
        '{"A":"冒泡排序","B":"选择排序","C":"快速排序","D":"插入排序"}',
        'C',
        '快速排序平均时间复杂度为 O(n log n)，冒泡、选择、插入排序均为 O(n²)。',
        '算法与数据结构',
        2
    ),
    (
        '10000000-0000-0000-0000-000000000003',
        '00000000-0000-0000-0000-000000000001',
        1,
        '关系型数据库中，主键（Primary Key）的主要作用是？',
        '{"A":"加速查询","B":"唯一标识一条记录","C":"建立外键关联","D":"对数据加密"}',
        'B',
        '主键用于唯一标识表中的每一行记录，不允许重复且不能为 NULL。',
        '数据库基础',
        1
    ),
    (
        '10000000-0000-0000-0000-000000000004',
        '00000000-0000-0000-0000-000000000001',
        1,
        'OSI 网络参考模型共分为几层？',
        '{"A":"4","B":"5","C":"6","D":"7"}',
        'D',
        'OSI 模型共 7 层：物理层、数据链路层、网络层、传输层、会话层、表示层、应用层。',
        '计算机网络',
        2
    ),
    (
        '10000000-0000-0000-0000-000000000005',
        '00000000-0000-0000-0000-000000000001',
        1,
        '以下哪个 IP 地址是无效地址？',
        '{"A":"192.168.1.1","B":"10.0.0.1","C":"256.0.0.1","D":"172.16.0.1"}',
        'C',
        'IPv4 每段取值范围为 0-255，256 超出范围，因此 256.0.0.1 是无效地址。',
        'IP协议',
        2
    );

-- ---------- 多选题（type = 2） ----------
INSERT INTO questions (id, tenant_id, type, content, options, correct_answer, explanation, knowledge_point, difficulty)
VALUES
    (
        '10000000-0000-0000-0000-000000000006',
        '00000000-0000-0000-0000-000000000001',
        2,
        '以下哪些属于关系型数据库？（多选）',
        '{"A":"MySQL","B":"MongoDB","C":"PostgreSQL","D":"Redis"}',
        'AC',
        'MySQL 和 PostgreSQL 均为关系型数据库；MongoDB 是文档型 NoSQL；Redis 是键值对内存数据库。',
        '数据库基础',
        2
    ),
    (
        '10000000-0000-0000-0000-000000000007',
        '00000000-0000-0000-0000-000000000001',
        2,
        '以下哪些 HTTP 状态码表示客户端错误？（多选）',
        '{"A":"200","B":"400","C":"404","D":"500"}',
        'BC',
        '4xx 系列表示客户端错误：400=Bad Request，404=Not Found；200=OK，500=服务器错误。',
        'HTTP协议',
        2
    ),
    (
        '10000000-0000-0000-0000-000000000008',
        '00000000-0000-0000-0000-000000000001',
        2,
        'TCP/IP 四层模型包含以下哪些层？（多选）',
        '{"A":"应用层","B":"表示层","C":"传输层","D":"网络层"}',
        'ACD',
        'TCP/IP 四层模型包含：应用层、传输层、网络层（互联网层）、网络接口层；表示层属于 OSI 模型特有。',
        '计算机网络',
        3
    );

-- ---------- 判断题（type = 3） ----------
INSERT INTO questions (id, tenant_id, type, content, options, correct_answer, explanation, knowledge_point, difficulty)
VALUES
    (
        '10000000-0000-0000-0000-000000000009',
        '00000000-0000-0000-0000-000000000001',
        3,
        'HTTP 是一种无状态协议。',
        '{"A":"正确","B":"错误"}',
        '正确',
        'HTTP 本身不保存客户端状态，每次请求都是独立的，需要 Cookie/Session 等机制来维持状态。',
        'HTTP协议',
        1
    ),
    (
        '10000000-0000-0000-0000-000000000010',
        '00000000-0000-0000-0000-000000000001',
        3,
        '二进制数 11111111 转换为十进制等于 255。',
        '{"A":"正确","B":"错误"}',
        '正确',
        '2^8 - 1 = 255，即 128+64+32+16+8+4+2+1 = 255。',
        '计算机基础',
        1
    ),
    (
        '10000000-0000-0000-0000-000000000011',
        '00000000-0000-0000-0000-000000000001',
        3,
        'TCP 协议不保证数据包按顺序到达目标主机。',
        '{"A":"正确","B":"错误"}',
        '错误',
        'TCP 提供可靠传输，通过序列号（Sequence Number）保证数据包按序交付给应用层。',
        'TCP协议',
        2
    );

-- ---------- 简答题（type = 4） ----------
INSERT INTO questions (id, tenant_id, type, content, options, correct_answer, explanation, knowledge_point, difficulty)
VALUES
    (
        '10000000-0000-0000-0000-000000000012',
        '00000000-0000-0000-0000-000000000001',
        4,
        '请简述数据库索引的概念及其在查询中的作用。',
        NULL,
        '索引是数据库中对一列或多列值进行排序的数据结构（常见如 B+ 树），能够显著加快数据检索速度，避免全表扫描。代价是占用额外存储空间，并在写操作时需要维护索引结构。',
        NULL,
        '数据库索引',
        3
    ),
    (
        '10000000-0000-0000-0000-000000000013',
        '00000000-0000-0000-0000-000000000001',
        4,
        '请描述 TCP 三次握手的完整过程，并说明为什么需要三次而不是两次。',
        NULL,
        '①客户端发送 SYN（seq=x）进入 SYN_SENT 状态；②服务端收到后回复 SYN+ACK（seq=y, ack=x+1）进入 SYN_RECEIVED；③客户端回复 ACK（ack=y+1），双方进入 ESTABLISHED 状态。两次握手无法确认客户端的接收能力，也无法防止历史重复连接请求，因此需要三次。',
        NULL,
        'TCP协议',
        4
    );

-- =============================================================================
-- Demo 数据：试卷
-- =============================================================================
INSERT INTO exam_papers (id, tenant_id, title, description, total_score, duration_minutes, status, start_time, end_time)
VALUES (
    '20000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    '计算机基础知识综合测试（第一期）',
    '涵盖计算机网络、数据库、算法基础等内容，适合期末综合考核。',
    100,
    90,
    1,  -- 已发布
    NOW() - INTERVAL '7 days',
    NOW() + INTERVAL '23 days'
) ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- Demo 数据：试卷题目关联（总分 100 分）
-- 单选 5×5=25，多选 3×10=30，判断 3×5=15，简答 2×15=30
-- =============================================================================
INSERT INTO exam_questions (exam_paper_id, question_id, score, "order") VALUES
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001',  5,  1),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002',  5,  2),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000003',  5,  3),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000004',  5,  4),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000005',  5,  5),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000006', 10,  6),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000007', 10,  7),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000008', 10,  8),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000009',  5,  9),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000010',  5, 10),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000011',  5, 11),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000012', 15, 12),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000013', 15, 13)
ON CONFLICT DO NOTHING;

-- =============================================================================
-- Demo 数据：学生答题记录（student001 参加上述试卷）
-- =============================================================================
INSERT INTO student_answers
    (exam_paper_id, question_id, student_id, answer_content, score, grading_status, submitted_at)
VALUES
    -- 单选题（自动评分）
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'student001', 'A',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 'student001', 'B',  0, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000003', 'student001', 'B',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000004', 'student001', 'D',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000005', 'student001', 'C',  5, 1, NOW() - INTERVAL '2 days'),
    -- 多选题（自动评分）
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000006', 'student001', 'AC',  10, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000007', 'student001', 'BCD',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000008', 'student001', 'ACD', 10, 1, NOW() - INTERVAL '2 days'),
    -- 判断题（自动评分）
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000009', 'student001', '正确',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000010', 'student001', '正确',  5, 1, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000011', 'student001', '正确',  0, 1, NOW() - INTERVAL '2 days'),
    -- 简答题（待 AI 评分）
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000012', 'student001',
        '索引是对数据库表中一列或多列的值进行排序的结构，可以帮助快速查找数据，就像书的目录一样，避免逐行扫描整张表。',
        NULL, 0, NOW() - INTERVAL '2 days'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000013', 'student001',
        '第一次握手：客户端发送SYN包请求建立连接。第二次握手：服务端收到后回复SYN+ACK。第三次握手：客户端发送ACK确认。需要三次是为了确保双方都能收发数据。',
        NULL, 0, NOW() - INTERVAL '2 days')
ON CONFLICT (exam_paper_id, student_id, question_id) DO NOTHING;

-- =============================================================================
-- Demo 数据：站内信（学生向教师求助）
-- =============================================================================
INSERT INTO messages (id, tenant_id, sender_id, sender_name, recipient_id, recipient_name, subject, body, attached_question_ids, is_read, created_at, updated_at)
VALUES
    -- 学生 student001 → 教师 teacher01：求助简答题
    (
        'c0000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000004',  -- student001
        'student001',
        'a0000000-0000-0000-0000-000000000003',  -- teacher01
        'teacher01',
        '关于数据库索引的疑问',
        '老师好，我在练习中做到了"请简述数据库索引的概念及其在查询中的作用"这道题，我的回答感觉不够全面，能否帮我详细解释一下 B+ 树索引和普通 B 树索引的区别？谢谢！',
        '["10000000-0000-0000-0000-000000000012"]',
        FALSE,
        NOW() - INTERVAL '1 day',
        NOW() - INTERVAL '1 day'
    ),
    -- 教师 teacher01 → 学生 student001：回复
    (
        'c0000000-0000-0000-0000-000000000002',
        '00000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000003',  -- teacher01
        'teacher01',
        'a0000000-0000-0000-0000-000000000004',  -- student001
        'student001',
        'Re: 关于数据库索引的疑问',
        '同学你好！B+ 树所有数据存在叶子节点且叶子节点通过链表相连，非常适合范围查询；而 B 树的数据分散在各层节点，适合单点查询。数据库（如 MySQL InnoDB、PostgreSQL）几乎都采用 B+ 树作为索引结构，原因就是叶子链表可以高效支持 ORDER BY 和 BETWEEN 等操作。继续加油！',
        NULL,
        TRUE,
        NOW() - INTERVAL '20 hours',
        NOW() - INTERVAL '20 hours'
    ),
    -- 学生 student001 → 教师 teacher01：求助 TCP 握手题
    (
        'c0000000-0000-0000-0000-000000000003',
        '00000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000004',  -- student001
        'student001',
        'a0000000-0000-0000-0000-000000000003',  -- teacher01
        'teacher01',
        '请问 TCP 三次握手为什么必须是三次？',
        '老师，我理解了三次握手的步骤，但还是不太明白为什么两次不行？两次不也能确认连接了吗？',
        '["10000000-0000-0000-0000-000000000013"]',
        FALSE,
        NOW() - INTERVAL '3 hours',
        NOW() - INTERVAL '3 hours'
    )
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- Demo 数据：图书
-- =============================================================================
INSERT INTO books (id, tenant_id, title, author, publisher, description, category, tags, publish_year, isbn, page_count, is_active, uploaded_by_id, uploaded_by_name)
VALUES
    (
        'd0000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        '计算机网络：自顶向下方法（第7版）',
        'James F. Kurose, Keith W. Ross',
        '机械工业出版社',
        '本书是计算机网络领域的经典教材，采用自顶向下的方法讲解计算机网络原理，从应用层开始，逐步深入到物理层。内容覆盖 HTTP、DNS、TCP/IP、路由算法等核心知识。',
        '计算机基础',
        '["计算机网络","TCP/IP","HTTP","经典教材"]',
        2018,
        '978-7-111-59971-1',
        792,
        TRUE,
        'a0000000-0000-0000-0000-000000000002',
        'demo_admin'
    ),
    (
        'd0000000-0000-0000-0000-000000000002',
        '00000000-0000-0000-0000-000000000001',
        '数据库系统概论（第5版）',
        '王珊, 萨师煊',
        '高等教育出版社',
        '本书是国内数据库领域的权威教材，系统介绍关系数据库理论、SQL 语言、数据库设计与规范化、事务管理、并发控制与恢复等核心内容。',
        '数据库',
        '["数据库","SQL","关系代数","事务","索引","经典教材"]',
        2014,
        '978-7-04-040664-1',
        636,
        TRUE,
        'a0000000-0000-0000-0000-000000000002',
        'demo_admin'
    ),
    (
        'd0000000-0000-0000-0000-000000000003',
        '00000000-0000-0000-0000-000000000001',
        '算法导论（第3版）',
        'Thomas H. Cormen 等',
        '机械工业出版社',
        '被誉为算法领域的"圣经"，系统介绍算法设计与分析方法，包括排序、数据结构、图算法、动态规划、NP完全性等经典内容，是计算机科学专业必读书目。',
        '算法与数据结构',
        '["算法","数据结构","排序","动态规划","图论","经典教材"]',
        2013,
        '978-7-111-40701-0',
        1084,
        TRUE,
        'a0000000-0000-0000-0000-000000000002',
        'demo_admin'
    ),
    (
        'd0000000-0000-0000-0000-000000000004',
        '00000000-0000-0000-0000-000000000001',
        'Clean Code：代码整洁之道',
        'Robert C. Martin',
        '人民邮电出版社',
        '本书提炼了软件工程中关于编写整洁代码的最佳实践，涵盖命名规范、函数设计、注释规范、格式化、错误处理、测试驱动开发等内容，是每位开发者必读的软件工程经典。',
        '软件工程',
        '["软件工程","代码规范","重构","最佳实践"]',
        2020,
        '978-7-115-51362-6',
        464,
        TRUE,
        'a0000000-0000-0000-0000-000000000003',
        'teacher01'
    )
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- Demo 数据：图书标注（student001 对《计算机网络》的标注示例）
-- =============================================================================
INSERT INTO book_annotations (id, book_id, user_id, user_name, page_number, selected_text, note, annotation_type, ai_question, ai_answer, highlight_color)
VALUES
    -- 书签
    (
        'e0000000-0000-0000-0000-000000000001',
        'd0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000004',
        'student001',
        42,
        NULL,
        '第2章 HTTP 协议详解，重点复习',
        1,
        NULL,
        NULL,
        '#FFEB3B'
    ),
    -- 阅读备注
    (
        'e0000000-0000-0000-0000-000000000002',
        'd0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000004',
        'student001',
        55,
        'HTTP是一种无状态协议。每个请求彼此独立，服务器不记录客户端的请求状态。',
        '重点：无状态特性 → Cookie/Session 用来弥补这一不足',
        2,
        NULL,
        NULL,
        '#A5D6A7'
    ),
    -- AI 问答
    (
        'e0000000-0000-0000-0000-000000000003',
        'd0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000004',
        'student001',
        72,
        'TCP三次握手建立连接过程：SYN → SYN+ACK → ACK',
        NULL,
        3,
        '为什么TCP需要三次握手而不是两次握手？',
        '两次握手无法确认客户端的接收能力，也无法防止历史旧连接报文干扰新连接。三次握手确保双方均具备发送和接收能力，同时可以同步双方的初始序列号（ISN），是建立可靠连接的最小代价方案。',
        '#CE93D8'
    )
ON CONFLICT (id) DO NOTHING;
