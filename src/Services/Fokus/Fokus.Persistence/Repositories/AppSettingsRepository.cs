namespace Fokus.Persistence.Repositories;

public class AppSettingsRepository(FokusDbContext db)
{
    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await db.AppSettings.SingleOrDefaultAsync(s => s.Id == 1, ct);
        if (settings is null)
        {
            settings = AppSettings.CreateDefault();
            db.AppSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        settings.Id = 1;
        var existing = await db.AppSettings.SingleOrDefaultAsync(s => s.Id == 1, ct);
        if (existing is null)
        {
            db.AppSettings.Add(settings);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(settings);
            existing.HealthThresholds = settings.HealthThresholds;
            existing.HealthWeights = settings.HealthWeights;
            existing.DoneStatuses = settings.DoneStatuses;
            existing.WorkflowStages = settings.WorkflowStages;
            existing.ExcludedFromScopeStatuses = settings.ExcludedFromScopeStatuses;
        }
        await db.SaveChangesAsync(ct);
    }
}
