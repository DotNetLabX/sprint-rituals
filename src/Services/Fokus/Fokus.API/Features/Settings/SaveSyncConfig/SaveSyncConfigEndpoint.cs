namespace Fokus.API.Features.Settings.SaveSyncConfig;

[HttpPut("/api/settings/sync-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveSyncConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveSyncConfigCommand, SaveSyncConfigResponse>
{
    public override async Task HandleAsync(SaveSyncConfigCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        req.Adapt(existing);
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveSyncConfigResponse { Success = true }, ct);
    }
}
