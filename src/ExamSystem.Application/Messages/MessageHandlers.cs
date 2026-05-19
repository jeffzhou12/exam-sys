using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
    Guid? AttachedExamPaperId = null);

public class SendMessageCommandHandler(IApplicationDbContext context)
{
    public async Task<Guid> Handle(
        SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        // 校验接收者存在且是教师
        var recipient = await context.Users.AsNoTracking()
            .Where(u => u.Id == command.RecipientId
                && u.TenantId == command.TenantId
                && u.Role == UserRole.Teacher
                && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("接收者不存在或不是本租户的教师。");

        var message = new Message
        {
            TenantId = command.TenantId,
            SenderId = command.SenderId,
            SenderName = command.SenderName,
            RecipientId = command.RecipientId,
            RecipientName = recipient.Username,
            Subject = command.Subject,
            Body = command.Body,
            AttachedQuestionIds = command.AttachedQuestionIds?.Count > 0
                ? JsonSerializer.Serialize(command.AttachedQuestionIds)
                : null,
            AttachedExamPaperId = command.AttachedExamPaperId,
            IsRead = false,
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
        return message.Id;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 获取消息列表（收件箱 / 发件箱）
// ─────────────────────────────────────────────────────────────────────────────

public record GetMessagesQuery(Guid? TenantId, Guid UserId, bool IsInbox, int Page = 1, int PageSize = 20);

public record MessageDto(
    Guid Id,
    string SenderName,
    string RecipientName,
    string Subject,
    string Body,
    List<Guid>? AttachedQuestionIds,
    Guid? AttachedExamPaperId,
    bool IsRead,
    DateTime CreatedAt);

public class GetMessagesQueryHandler(IApplicationDbContext context)
{
    public async Task<List<MessageDto>> Handle(
        GetMessagesQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.Messages.AsNoTracking();

        if (query.TenantId.HasValue)
            q = q.Where(m => m.TenantId == query.TenantId.Value);

        q = query.IsInbox
            ? q.Where(m => m.RecipientId == query.UserId)
            : q.Where(m => m.SenderId == query.UserId);

        var items = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return items.Select(m => new MessageDto(
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
            m.CreatedAt)).ToList();
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
