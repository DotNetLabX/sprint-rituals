namespace Fokus.API.Features.Settings.SaveHealthConfig;

[HttpPut("/api/settings/health-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveHealthConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveHealthConfigCommand, SaveHealthConfigResponse>
{
    public override async Task HandleAsync(SaveHealthConfigCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        req.Adapt(existing);
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveHealthConfigResponse { Success = true }, ct);
    }
}
