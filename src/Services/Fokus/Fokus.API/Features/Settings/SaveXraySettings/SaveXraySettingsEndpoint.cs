namespace Fokus.API.Features.Settings.SaveXraySettings;

[HttpPut("/api/settings/xray")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveXraySettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveXraySettingsCommand, SaveXraySettingsResponse>
{
    public override async Task HandleAsync(SaveXraySettingsCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);

        existing.XrayEnabled = req.XrayEnabled;
        existing.XrayClientId = req.XrayClientId;

        // Secret preservation: only overwrite if a non-empty value is provided
        if (!string.IsNullOrEmpty(req.XrayClientSecret))
            existing.XrayClientSecret = req.XrayClientSecret;

        await repository.SaveAsync(existing, ct);

        await Send.OkAsync(new SaveXraySettingsResponse { Success = true }, ct);
    }
}
