using ExamSystem.Domain.Entities;

namespace ExamSystem.Application.Common.Interfaces;

public interface ISmsTemplateService
{
    Task<SmsTemplate?> ResolveAsync(Guid? tenantId, string scene, CancellationToken ct = default);
    Task<List<SmsTemplate>> GetTemplatesAsync(Guid? tenantId, CancellationToken ct = default);
    Task<SmsTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SmsTemplate> CreateAsync(SmsTemplate template, CancellationToken ct = default);
    Task<SmsTemplate> UpdateAsync(SmsTemplate template, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}