namespace Fokus.API.Features.Settings.SaveDoneStatuses;

[HttpPut("/api/settings/done-statuses")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveDoneStatusesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveDoneStatusesCommand, List<string>>
{
    public override async Task HandleAsync(SaveDoneStatusesCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.DoneStatuses = req.Statuses;
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(existing.DoneStatuses, ct);
    }
}
