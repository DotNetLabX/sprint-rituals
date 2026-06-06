namespace Fokus.API.Features.Settings.SaveAnalyticsTargets;

[HttpPut("/api/settings/analytics-targets")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveAnalyticsTargetsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveAnalyticsTargetsCommand, SaveAnalyticsTargetsResponse>
{
    public override async Task HandleAsync(SaveAnalyticsTargetsCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        req.Adapt(existing);
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveAnalyticsTargetsResponse { Success = true }, ct);
    }
}
