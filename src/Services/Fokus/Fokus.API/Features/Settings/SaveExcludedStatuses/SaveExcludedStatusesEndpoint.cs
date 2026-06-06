namespace Fokus.API.Features.Settings.SaveExcludedStatuses;

[HttpPut("/api/settings/excluded-statuses")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveExcludedStatusesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveExcludedStatusesCommand, List<string>>
{
    public override async Task HandleAsync(SaveExcludedStatusesCommand req, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);
        settings.ExcludedFromScopeStatuses = req.Statuses;
        await repository.SaveAsync(settings, ct);
        await Send.OkAsync(settings.ExcludedFromScopeStatuses, ct);
    }
}
