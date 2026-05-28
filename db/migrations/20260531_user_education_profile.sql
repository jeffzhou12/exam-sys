-- Migration: 20260531_user_education_profile
-- 用户学历与学科兴趣扩展字段

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS education_level     VARCHAR(50),
    ADD COLUMN IF NOT EXISTS interested_subjects JSONB;

COMMENT ON COLUMN users.education_level IS '学历：小学/初中/高中/大学/研究生/博士';
COMMENT ON COLUMN users.interested_subjects IS '感兴趣的学科列表，JSON 字符串数组，如 ["数学","物理"]';
