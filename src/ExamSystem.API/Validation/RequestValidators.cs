using ExamSystem.API.Controllers;
using ExamSystem.Domain.Enums;
using FluentValidation;

namespace ExamSystem.API.Validation;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空。")
            .MaximumLength(100).WithMessage("用户名最长 100 个字符。");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空。")
            .MinimumLength(6).WithMessage("密码长度至少 6 位。")
            .MaximumLength(128).WithMessage("密码最长 128 个字符。");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空。")
            .Length(2, 50).WithMessage("用户名长度为 2~50 个字符。")
            .Matches(@"^[a-zA-Z0-9_\u4e00-\u9fa5]+$").WithMessage("用户名只能包含字母、数字、下划线或中文。");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空。")
            .MinimumLength(8).WithMessage("密码长度至少 8 位。")
            .MaximumLength(128).WithMessage("密码最长 128 个字符。")
            .Matches(@"[A-Z]").WithMessage("密码必须包含至少一个大写字母。")
            .Matches(@"[0-9]").WithMessage("密码必须包含至少一个数字。");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确。")
            .MaximumLength(320).WithMessage("邮箱最长 320 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("用户名或邮箱不能为空。")
            .MaximumLength(320).WithMessage("输入长度不能超过 320 个字符。");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("重置令牌不能为空。");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空。")
            .MinimumLength(8).WithMessage("新密码长度至少 8 位。")
            .MaximumLength(128).WithMessage("新密码最长 128 个字符。")
            .Matches(@"[A-Z]").WithMessage("新密码必须包含至少一个大写字母。")
            .Matches(@"[0-9]").WithMessage("新密码必须包含至少一个数字。");
    }
}

// ─── Questions ────────────────────────────────────────────────────────────────

public class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("题目内容不能为空。")
            .MaximumLength(4000).WithMessage("题目内容最长 4000 个字符。");

        RuleFor(x => x.CorrectAnswer)
            .NotEmpty().WithMessage("正确答案不能为空。")
            .MaximumLength(2000).WithMessage("正确答案最长 2000 个字符。");

        RuleFor(x => x.Difficulty)
            .InclusiveBetween(1, 5).WithMessage("难度系数必须在 1~5 之间。");

        RuleFor(x => x.Explanation)
            .MaximumLength(2000).WithMessage("解析最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Explanation));

        RuleFor(x => x.KnowledgePoint)
            .MaximumLength(500).WithMessage("知识点最长 500 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.KnowledgePoint));

        // 单选/多选/判断题必须提供选项
        RuleFor(x => x.Options)
            .NotNull().WithMessage("选择题/判断题必须提供选项（Options）。")
            .When(x => x.Type is QuestionType.SingleChoice or QuestionType.MultipleChoice or QuestionType.TrueFalse);
    }
}

public class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("题目内容不能为空。")
            .MaximumLength(4000).WithMessage("题目内容最长 4000 个字符。");

        RuleFor(x => x.CorrectAnswer)
            .NotEmpty().WithMessage("正确答案不能为空。")
            .MaximumLength(2000).WithMessage("正确答案最长 2000 个字符。");

        RuleFor(x => x.Difficulty)
            .InclusiveBetween(1, 5).WithMessage("难度系数必须在 1~5 之间。");

        RuleFor(x => x.Explanation)
            .MaximumLength(2000).WithMessage("解析最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Explanation));

        RuleFor(x => x.KnowledgePoint)
            .MaximumLength(500).WithMessage("知识点最长 500 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.KnowledgePoint));
    }
}

public class GenerateQuestionsRequestValidator : AbstractValidator<GenerateQuestionsRequest>
{
    public GenerateQuestionsRequestValidator()
    {
        RuleFor(x => x.KnowledgePoint)
            .NotEmpty().WithMessage("知识点不能为空。")
            .MaximumLength(500).WithMessage("知识点最长 500 个字符。");

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 50).WithMessage("生成数量必须在 1~50 之间。");
    }
}

public class PreviewAiQuestionsRequestValidator : AbstractValidator<PreviewAiQuestionsRequest>
{
    public PreviewAiQuestionsRequestValidator()
    {
        RuleFor(x => x.KnowledgePoint)
            .NotEmpty().WithMessage("知识点不能为空。")
            .MaximumLength(500).WithMessage("知识点最长 500 个字符。");

        RuleFor(x => x.TypeConfigs)
            .NotEmpty().WithMessage("至少需要配置一种题型。")
            .Must(c => c.Sum(t => t.Count) is >= 1 and <= 100)
            .WithMessage("题目总数必须在 1~100 之间。");

        RuleForEach(x => x.TypeConfigs).ChildRules(cfg =>
        {
            cfg.RuleFor(c => c.Count)
               .InclusiveBetween(1, 50).WithMessage("单种题型生成数量必须在 1~50 之间。");
            cfg.RuleFor(c => c.Difficulty)
               .InclusiveBetween(1, 5).WithMessage("难度系数必须在 1~5 之间。");
        });
    }
}

// ─── ExamPapers ───────────────────────────────────────────────────────────────

public class CreateExamPaperRequestValidator : AbstractValidator<CreateExamPaperRequest>
{
    public CreateExamPaperRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("试卷标题不能为空。")
            .MaximumLength(500).WithMessage("试卷标题最长 500 个字符。");

        RuleFor(x => x.TotalScore)
            .GreaterThan(0).WithMessage("总分必须大于 0。")
            .LessThanOrEqualTo(1000).WithMessage("总分最大为 1000 分。");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 600).WithMessage("考试时长必须在 1~600 分钟之间。");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("描述最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("结束时间必须晚于开始时间。")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);

        RuleFor(x => x.Questions)
            .NotNull().WithMessage("试卷必须包含至少一道题目。");

        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(r => r.QuestionId)
             .NotEmpty().WithMessage("题目 ID 不能为空。");
            q.RuleFor(r => r.Score)
             .GreaterThan(0).WithMessage("每道题分值必须大于 0。");
        });
    }
}

// ─── StudentAnswers ───────────────────────────────────────────────────────────

public class SubmitAnswersRequestValidator : AbstractValidator<SubmitAnswersRequest>
{
    public SubmitAnswersRequestValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("学生 ID 不能为空。")
            .MaximumLength(100).WithMessage("学生 ID 最长 100 个字符。");

        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("答题记录不能为空。");

        RuleForEach(x => x.Answers).ChildRules(a =>
        {
            a.RuleFor(r => r.QuestionId)
             .NotEmpty().WithMessage("题目 ID 不能为空。");
            a.RuleFor(r => r.Content)
             .NotNull().WithMessage("答案内容不能为 null。")
             .MaximumLength(8000).WithMessage("答案内容最长 8000 个字符。");
        });
    }
}

public class ManualGradeRequestValidator : AbstractValidator<ManualGradeRequest>
{
    public ManualGradeRequestValidator()
    {
        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0).WithMessage("分数不能为负数。");

        RuleFor(x => x.Feedback)
            .MaximumLength(4000).WithMessage("评语最长 4000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Feedback));
    }
}

// ─── Messages ─────────────────────────────────────────────────────────────────

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("收件人 ID 不能为空。");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("主题不能为空。")
            .MaximumLength(500).WithMessage("主题最长 500 个字符。");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("消息内容不能为空。")
            .MaximumLength(4000).WithMessage("消息内容最长 4000 个字符。");

        RuleFor(x => x.AttachedQuestionIds)
            .Must(ids => ids == null || ids.Count <= 20)
            .WithMessage("附带题目最多 20 道。");
    }
}

// ─── Books ────────────────────────────────────────────────────────────────────

public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("书名不能为空。")
            .MaximumLength(500).WithMessage("书名最长 500 个字符。");

        RuleFor(x => x.Author)
            .MaximumLength(300).WithMessage("作者最长 300 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Author));

        RuleFor(x => x.Publisher)
            .MaximumLength(300).WithMessage("出版社最长 300 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Publisher));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("简介最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("分类最长 100 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));

        RuleFor(x => x.Isbn)
            .MaximumLength(30).WithMessage("ISBN 最长 30 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Isbn));

        RuleFor(x => x.PublishYear)
            .InclusiveBetween(1000, 2100).WithMessage("出版年份应在 1000~2100 之间。")
            .When(x => x.PublishYear.HasValue);

        RuleFor(x => x.Tags)
            .Must(t => t == null || t.Count <= 20).WithMessage("标签最多 20 个。")
            .Must(t => t == null || t.All(tag => tag.Length <= 50)).WithMessage("每个标签最长 50 个字符。");
    }
}

public class UpdateBookRequestValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("书名不能为空。")
            .MaximumLength(500).WithMessage("书名最长 500 个字符。");

        RuleFor(x => x.Author)
            .MaximumLength(300).WithMessage("作者最长 300 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Author));

        RuleFor(x => x.Publisher)
            .MaximumLength(300).WithMessage("出版社最长 300 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Publisher));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("简介最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("分类最长 100 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));

        RuleFor(x => x.Isbn)
            .MaximumLength(30).WithMessage("ISBN 最长 30 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Isbn));

        RuleFor(x => x.PublishYear)
            .InclusiveBetween(1000, 2100).WithMessage("出版年份应在 1000~2100 之间。")
            .When(x => x.PublishYear.HasValue);

        RuleFor(x => x.Tags)
            .Must(t => t == null || t.Count <= 20).WithMessage("标签最多 20 个。")
            .Must(t => t == null || t.All(tag => tag.Length <= 50)).WithMessage("每个标签最长 50 个字符。");
    }
}

public class CreateAnnotationRequestValidator : AbstractValidator<CreateAnnotationRequest>
{
    public CreateAnnotationRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("页码必须大于 0。");

        RuleFor(x => x.AnnotationType)
            .InclusiveBetween(1, 3).WithMessage("标注类型必须为 1（书签）、2（备注）或 3（AI问答）。");

        RuleFor(x => x.SelectedText)
            .MaximumLength(2000).WithMessage("选中文字最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.SelectedText));

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("备注内容最长 2000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.AiQuestion)
            .MaximumLength(1000).WithMessage("AI 提问最长 1000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.AiQuestion));
    }
}

public class AiAnalyzeRequestValidator : AbstractValidator<AiAnalyzeRequest>
{
    public AiAnalyzeRequestValidator()
    {
        RuleFor(x => x.SelectedText)
            .NotEmpty().WithMessage("待分析文字不能为空。")
            .MaximumLength(2000).WithMessage("待分析文字最长 2000 个字符。");

        RuleFor(x => x.Question)
            .MaximumLength(1000).WithMessage("问题最长 1000 个字符。")
            .When(x => !string.IsNullOrWhiteSpace(x.Question));
    }
}

// ─── Users ────────────────────────────────────────────────────────────────────

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空。")
            .Length(2, 50).WithMessage("用户名长度为 2~50 个字符。");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空。")
            .MinimumLength(8).WithMessage("密码长度至少 8 位。")
            .MaximumLength(128).WithMessage("密码最长 128 个字符。");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确。")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空。")
            .MinimumLength(8).WithMessage("新密码长度至少 8 位。")
            .MaximumLength(128).WithMessage("新密码最长 128 个字符。");
    }
}
