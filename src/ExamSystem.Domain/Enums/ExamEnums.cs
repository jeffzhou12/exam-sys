namespace ExamSystem.Domain.Enums;

public enum QuestionType
{
    SingleChoice = 1,   // 单选题
    MultipleChoice = 2, // 多选题
    TrueFalse = 3,      // 判断题
    ShortAnswer = 4     // 简答题
}

public enum ExamStatus
{
    Draft = 0,      // 草稿
    Published = 1,  // 已发布
    InProgress = 2, // 进行中
    Ended = 3,      // 已结束
    Cancelled = 4   // 已取消
}

public enum GradingStatus
{
    Pending = 0,     // 待评分
    AutoGraded = 1,  // 自动评分完成
    AiGraded = 2,    // AI 评分完成
    ManualGraded = 3 // 人工评分完成
}

public enum UserRole
{
    SuperAdmin = -1, // 超级管理员（无租户，可管理全部）
    Admin = 0,       // 普通管理员（归属某租户）
    Teacher = 1,     // 教师
    Student = 2      // 学生
}
