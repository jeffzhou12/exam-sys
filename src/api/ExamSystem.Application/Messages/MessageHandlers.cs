using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamSystem.Application.Messages;

// ─────────────────────────────────────────────────────────────────────────────
// 发送站内信
// ─────────────────────────────────────────────────────────────────────────────

public record SendMessageCommand(
    Guid TenantId,
    Guid SenderId,
    string SenderName,
    Guid RecipientId,
    string Subject,
    string Body,
    List<Guid>? AttachedQuestionIds = null,
    Guid? AttachedExamPaperId = null,
    Guid? ParentMessageId = null);

public class SendMessageCommandHandler(IApplicationDbContext context)
{
    public async Task<Guid> Handle(
        SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        Guid resolvedRecipientId;
        string recipientName;

        if (command.ParentMessageId.HasValue)
        {
            // 回复消息：校验根消息存在且当前用户是参与者
            var root = await context.Messages.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == command.ParentMessageId.Value
                      && m.TenantId == command.TenantId
                      && m.ParentMessageId == null,
                    cancellationToken)
                ?? throw new InvalidOperationException("原始消息不存在。");

            if (root.SenderId != command.SenderId && root.RecipientId != command.SenderId)
                throw new InvalidOperationException("无权回复该消息。");

            // 服务端自动推断回复接收者（对话中的另一方）
            resolvedRecipientId = root.SenderId == command.SenderId
                ? root.RecipientId
                : root.SenderId;
            recipientName = root.SenderId == command.SenderId
                ? root.RecipientName
                : root.SenderName;

            // 更新根消息的 updated_at 使其在列表中置顶
            var rootForUpdate = await context.Messages
                .FirstOrDefaultAsync(m => m.Id == command.ParentMessageId.Value, cancellationToken);
            if (rootForUpdate != null) rootForUpdate.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // 新建对话：校验接收者存在且是教师
            var recipient = await context.Users.AsNoTracking()
                .Where(u => u.Id == command.RecipientId
                    && u.TenantId == command.TenantId
                    && u.Role == UserRole.Teacher
                    && u.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("接收者不存在或不是本租户的教师。");

            resolvedRecipientId = recipient.Id;
            recipientName = recipient.Username;
        }

        var message = new Message
        {
            TenantId = command.TenantId,
            SenderId = command.SenderId,
            SenderName = command.SenderName,
            RecipientId = resolvedRecipientId,
            RecipientName = recipientName,
            Subject = command.Subject,
            Body = command.Body,
            AttachedQuestionIds = command.AttachedQuestionIds?.Count > 0
                ? JsonSerializer.Serialize(command.AttachedQuestionIds)
                : null,
            AttachedExamPaperId = command.AttachedExamPaperId,
            IsRead = false,
            ParentMessageId = command.ParentMessageId,
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
        return message.Id;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取消息列表（收件箱 / 发件箱），仅返回根消息 + 回复计数
// ─────────────────────────────────────────────────────────────────────────────

public record GetMessagesQuery(Guid? TenantId, Guid? UserId, bool IsInbox, int Page = 1, int PageSize = 20);

public record MessageDto(
    Guid Id,
    string SenderName,
    string RecipientName,
    string Subject,
    string Body,
    List<Guid>? AttachedQuestionIds,
    Guid? AttachedExamPaperId,
    bool IsRead,
    DateTime CreatedAt,
    int ReplyCount = 0,
    DateTime? LatestReplyAt = null);

public class GetMessagesQueryHandler(IApplicationDbContext context)
{
    public async Task<List<MessageDto>> Handle(
        GetMessagesQuery query, CancellationToken cancellationToken = default)
    {
        // 只返回根消息（ParentMessageId == null）
        var q = context.Messages.AsNoTracking()
            .Where(m => m.ParentMessageId == null);

        if (query.TenantId.HasValue)
            q = q.Where(m => m.TenantId == query.TenantId.Value);

        if (query.UserId.HasValue)
        {
            q = query.IsInbox
                ? q.Where(m => m.RecipientId == query.UserId.Value)
                : q.Where(m => m.SenderId == query.UserId.Value);
        }

        var items = await q
            .OrderByDescending(m => m.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var rootIds = items.Select(m => m.Id).ToList();

        // 批量获取回复计数和最新回复时间
        var replyCounts = await context.Messages.AsNoTracking()
            .Where(m => m.ParentMessageId != null && rootIds.Contains(m.ParentMessageId!.Value))
            .GroupBy(m => m.ParentMessageId!.Value)
            .Select(g => new { RootId = g.Key, Count = g.Count(), Latest = g.Max(x => x.CreatedAt) })
            .ToListAsync(cancellationToken);

        var replyMap = replyCounts.ToDictionary(x => x.RootId);

        return items.Select(m =>
        {
            replyMap.TryGetValue(m.Id, out var r);
            return new MessageDto(
                m.Id,
                m.SenderName,
                m.RecipientName,
                m.Subject,
                m.Body,
                m.AttachedQuestionIds != null
                    ? JsonSerializer.Deserialize<List<Guid>>(m.AttachedQuestionIds)
                    : null,
                m.AttachedExamPaperId,
                m.IsRead,
                m.CreatedAt,
                r?.Count ?? 0,
                r?.Latest);
        }).ToList();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取对话线程（根消息 + 全部回复，按时间升序）
// ─────────────────────────────────────────────────────────────────────────────

public record ThreadMessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string Body,
    List<Guid>? AttachedQuestionIds,
    Guid? AttachedExamPaperId,
    bool IsRead,
    DateTime CreatedAt,
    bool IsRoot);

public class GetMessageThreadQueryHandler(IApplicationDbContext context)
{
    public async Task<List<ThreadMessageDto>?> Handle(
        Guid rootMessageId,
        Guid? tenantId,
        Guid userId,
        bool canViewAllInTenant = false,
        CancellationToken cancellationToken = default)
    {
        var rootQuery = context.Messages.AsNoTracking()
            .Where(m => m.Id == rootMessageId && m.ParentMessageId == null);
        if (tenantId.HasValue)
            rootQuery = rootQuery.Where(m => m.TenantId == tenantId.Value);

        var root = await rootQuery.FirstOrDefaultAsync(cancellationToken);

        if (root is null) return null;

        // 当前用户必须是这个对话的参与者
        if (!canViewAllInTenant && root.SenderId != userId && root.RecipientId != userId)
            return null;

        var replies = await context.Messages.AsNoTracking()
            .Where(m => m.ParentMessageId == rootMessageId && m.TenantId == root.TenantId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        // 将当前用户的回复标记为已读
        var unread = await context.Messages
            .Where(m => m.ParentMessageId == rootMessageId
                && m.RecipientId == userId && !m.IsRead)
            .ToListAsync(cancellationToken);
        if (!root.IsRead && root.RecipientId == userId)
        {
            var rootForUpdate = await context.Messages.FindAsync([rootMessageId], cancellationToken);
            if (rootForUpdate != null) rootForUpdate.IsRead = true;
        }
        foreach (var u in unread) u.IsRead = true;
        if (unread.Count > 0) await context.SaveChangesAsync(cancellationToken);

        var all = new List<Message> { root };
        all.AddRange(replies);

        return all.Select(m => new ThreadMessageDto(
            m.Id,
            m.SenderId,
            m.SenderName,
            m.Body,
            m.AttachedQuestionIds != null
                ? JsonSerializer.Deserialize<List<Guid>>(m.AttachedQuestionIds)
                : null,
            m.AttachedExamPaperId,
            m.IsRead,
            m.CreatedAt,
            IsRoot: m.Id == rootMessageId)).ToList();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取消息的关联题目（学生有权限调用，仅返回该对话的附题）
// ─────────────────────────────────────────────────────────────────────────────

public record MessageQuestionDto(
    Guid Id,
    string Content,
    string QuestionType,
    string Difficulty,
    string? KnowledgePoint,
    List<string>? Options,
    string? Answer,
    string? Explanation);

public class GetMessageQuestionsQueryHandler(IApplicationDbContext context)
{
    public async Task<List<MessageQuestionDto>?> Handle(
        Guid messageId,
        Guid? tenantId,
        Guid userId,
        bool canViewAllInTenant = false,
        CancellationToken cancellationToken = default)
    {
        var messageQuery = context.Messages.AsNoTracking()
            .Where(m => m.Id == messageId);
        if (tenantId.HasValue)
            messageQuery = messageQuery.Where(m => m.TenantId == tenantId.Value);

        var message = await messageQuery.FirstOrDefaultAsync(cancellationToken);

        if (message is null) return null;

        // 归一化到根消息来校验权限
        var rootId = message.ParentMessageId ?? message.Id;
        var root = message.ParentMessageId.HasValue
            ? await context.Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == rootId && m.TenantId == message.TenantId, cancellationToken)
            : message;

        if (root is null) return null;
        if (!canViewAllInTenant && root.SenderId != userId && root.RecipientId != userId)
            return null;

        if (string.IsNullOrEmpty(message.AttachedQuestionIds))
            return [];

        var questionIds = JsonSerializer.Deserialize<List<Guid>>(message.AttachedQuestionIds);
        if (questionIds is null || questionIds.Count == 0)
            return [];

        var questions = await context.Questions.AsNoTracking()
            .Where(q => questionIds.Contains(q.Id) && q.TenantId == message.TenantId)
            .ToListAsync(cancellationToken);

        return questions.Select(q => new MessageQuestionDto(
            q.Id,
            q.Content,
            q.Type.ToString(),
            q.Difficulty.ToString(),
            q.KnowledgePoint,
            ParseOptions(q.Options),
            q.CorrectAnswer,
            q.Explanation)).ToList();
    }

    // Options 可能是 JSON 数组 ["A","B",...] 或对象 {"A":"...","B":"..."}
    private static List<string>? ParseOptions(JsonDocument? doc)
    {
        if (doc is null) return null;
        var root = doc.RootElement;
        return root.ValueKind switch
        {
            JsonValueKind.Array =>
                root.EnumerateArray()
                    .Select(e => e.GetString() ?? e.GetRawText())
                    .ToList(),
            JsonValueKind.Object =>
                root.EnumerateObject()
                    .Select(p => p.Value.GetString() ?? p.Value.GetRawText())
                    .ToList(),
            _ => null
        };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 标记已读
// ─────────────────────────────────────────────────────────────────────────────

public record MarkMessageReadCommand(Guid? TenantId, Guid UserId, Guid MessageId);

public class MarkMessageReadCommandHandler(IApplicationDbContext context)
{
    public async Task Handle(
        MarkMessageReadCommand command, CancellationToken cancellationToken = default)
    {
        var q = context.Messages.Where(m => m.Id == command.MessageId
                && m.RecipientId == command.UserId);

        if (command.TenantId.HasValue)
            q = q.Where(m => m.TenantId == command.TenantId.Value);

        var msg = await q.FirstOrDefaultAsync(cancellationToken);

        if (msg is null) return;
        msg.IsRead = true;
        await context.SaveChangesAsync(cancellationToken);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取租户内的教师列表（发消息时选择收件人）
// ─────────────────────────────────────────────────────────────────────────────

public record TeacherDto(Guid Id, string Username);

public class GetTenantTeachersQueryHandler(IApplicationDbContext context)
{
    public async Task<List<TeacherDto>> Handle(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId
                && u.Role == UserRole.Teacher
                && u.IsActive)
            .OrderBy(u => u.Username)
            .Select(u => new TeacherDto(u.Id, u.Username))
            .ToListAsync(cancellationToken);
    }
}
