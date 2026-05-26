using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Infrastructure.Auth;

public class SmsTemplateService(ApplicationDbContext db) : ISmsTemplateService
{
    public async Task<SmsTemplate?> ResolveAsync(Guid? tenantId, string scene, CancellationToken ct = default)
    {
        var normalizedScene = scene.Trim();

        return await db.SmsTemplates
            .AsNoTracking()
            .Where(t => t.IsEnabled
                && (t.TenantId == tenantId || t.TenantId == null)
                && t.Scene == normalizedScene)
            .OrderByDescending(t => t.TenantId != null)
            .ThenByDescending(t => t.Priority)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<SmsTemplate>> GetTemplatesAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.SmsTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Scene)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<SmsTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.SmsTemplates.FindAsync([id], ct);

    public async Task<SmsTemplate> CreateAsync(SmsTemplate template, CancellationToken ct = default)
    {
        db.SmsTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    public async Task<SmsTemplate> UpdateAsync(SmsTemplate template, CancellationToken ct = default)
    {
        db.SmsTemplates.Update(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await db.SmsTemplates.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"短信模板 {id} 不存在");
        db.SmsTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
    }
}