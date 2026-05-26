using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Application.SmsTemplates;

public record SmsTemplateDto(
    Guid Id,
    Guid? TenantId,
    string Scene,
    string Name,
    string TemplateBody,
    bool IsEnabled,
    int Priority,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GetSmsTemplatesQuery(Guid? TenantId);

public class GetSmsTemplatesQueryHandler(ISmsTemplateService templateService)
{
    public async Task<List<SmsTemplateDto>> Handle(GetSmsTemplatesQuery query, CancellationToken ct = default)
        => (await templateService.GetTemplatesAsync(query.TenantId, ct)).Select(ToDto).ToList();

    internal static SmsTemplateDto ToDto(SmsTemplate template) => new(
        template.Id,
        template.TenantId,
        template.Scene,
        template.Name,
        template.TemplateBody,
        template.IsEnabled,
        template.Priority,
        template.Description,
        template.CreatedAt,
        template.UpdatedAt);
}

public record GetSmsTemplateByIdQuery(Guid Id);

public class GetSmsTemplateByIdQueryHandler(ISmsTemplateService templateService)
{
    public async Task<SmsTemplateDto?> Handle(GetSmsTemplateByIdQuery query, CancellationToken ct = default)
    {
        var template = await templateService.GetByIdAsync(query.Id, ct);
        return template is null ? null : GetSmsTemplatesQueryHandler.ToDto(template);
    }
}

public record CreateSmsTemplateCommand(
    Guid? TenantId,
    string Scene,
    string Name,
    string TemplateBody,
    bool IsEnabled,
    int Priority,
    string? Description);

public class CreateSmsTemplateCommandHandler(ISmsTemplateService templateService)
{
    public async Task<Guid> Handle(CreateSmsTemplateCommand command, CancellationToken ct = default)
    {
        var template = new SmsTemplate
        {
            TenantId = command.TenantId,
            Scene = command.Scene.Trim(),
            Name = command.Name.Trim(),
            TemplateBody = command.TemplateBody.Trim(),
            IsEnabled = command.IsEnabled,
            Priority = command.Priority,
            Description = command.Description?.Trim()
        };

        var created = await templateService.CreateAsync(template, ct);
        return created.Id;
    }
}

public record UpdateSmsTemplateCommand(
    Guid Id,
    string Scene,
    string Name,
    string TemplateBody,
    bool IsEnabled,
    int Priority,
    string? Description);

public class UpdateSmsTemplateCommandHandler(ISmsTemplateService templateService)
{
    public async Task Handle(UpdateSmsTemplateCommand command, CancellationToken ct = default)
    {
        var template = await templateService.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"短信模板 {command.Id} 不存在");

        template.Scene = command.Scene.Trim();
        template.Name = command.Name.Trim();
        template.TemplateBody = command.TemplateBody.Trim();
        template.IsEnabled = command.IsEnabled;
        template.Priority = command.Priority;
        template.Description = command.Description?.Trim();

        await templateService.UpdateAsync(template, ct);
    }
}

public record DeleteSmsTemplateCommand(Guid Id);

public class DeleteSmsTemplateCommandHandler(ISmsTemplateService templateService)
{
    public Task Handle(DeleteSmsTemplateCommand command, CancellationToken ct = default)
        => templateService.DeleteAsync(command.Id, ct);
}