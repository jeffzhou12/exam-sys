-- 错题本表：记录学生在练习中答错的题目，支持 upsert 累计错误次数
CREATE TABLE IF NOT EXISTS wrong_book_items (
    id           uuid                     NOT NULL DEFAULT gen_random_uuid(),
    tenant_id    uuid                     NOT NULL,
    student_id   text                     NOT NULL,
    question_id  uuid                     NOT NULL,
    answer_given text                     NOT NULL DEFAULT '',
    wrong_count  integer                  NOT NULL DEFAULT 1,
    created_at   timestamp with time zone NOT NULL DEFAULT now(),
    updated_at   timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_wrong_book_items PRIMARY KEY (id),
    CONSTRAINT fk_wrong_book_items_questions_question_id
        FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
);

-- 唯一索引：每个学生每道题只保留一条记录
CREATE UNIQUE INDEX IF NOT EXISTS ix_wrong_book_items_tenant_student_question
    ON wrong_book_items (tenant_id, student_id, question_id);

-- 查询索引：按租户+学生查询
CREATE INDEX IF NOT EXISTS ix_wrong_book_items_tenant_id_student_id
    ON wrong_book_items (tenant_id, student_id);
