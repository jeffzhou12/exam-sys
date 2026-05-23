-- 题目富文本支持升级：将题干、答案、解析扩展为 TEXT
ALTER TABLE questions
    ALTER COLUMN content TYPE TEXT,
    ALTER COLUMN correct_answer TYPE TEXT,
    ALTER COLUMN explanation TYPE TEXT;
