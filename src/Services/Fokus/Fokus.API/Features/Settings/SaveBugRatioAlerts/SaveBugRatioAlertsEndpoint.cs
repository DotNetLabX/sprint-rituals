namespace Fokus.API.Features.Settings.SaveBugRatioAlerts;

[HttpPut("/api/settings/bug-ratio-alerts")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveBugRatioAlertsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveBugRatioAlertsCommand, SaveBugRatioAlertsResponse>
{
    public override async Task HandleAsync(SaveBugRatioAlertsCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        req.Adapt(existing);
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveBugRatioAlertsResponse { Success = true }, ct);
    }
}
