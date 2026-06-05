namespace Fokus.API.Features.Settings.SaveXraySettings;

public class SaveXraySettingsRequest
{
    public bool XrayEnabled { get; set; }
    public string? XrayClientId { get; set; }
    public string? XrayClientSecret { get; set; }
}

public class SaveXraySettingsRequestValidator : Validator<SaveXraySettingsRequest>
{
    public SaveXraySettingsRequestValidator()
    {
        RuleFor(x => x.XrayClientId)
            .NotEmpty()
            .WithMessage("Client ID is required when Xray is enabled.")
            .When(x => x.XrayEnabled);
    }
}

[HttpPut("/api/settings/xray")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveXraySettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveXraySettingsRequest, SaveXraySettingsResponse>
{
    public override async Task HandleAsync(SaveXraySettingsRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);

        existing.XrayEnabled = req.XrayEnabled;
        existing.XrayClientId = req.XrayClientId;

        // Secret preservation: only overwrite if a non-empty value is provided
        if (!string.IsNullOrEmpty(req.XrayClientSecret))
            existing.XrayClientSecret = req.XrayClientSecret;

        await repository.SaveAsync(existing, ct);

        await SendOkAsync(new SaveXraySettingsResponse { Success = true }, ct);
    }
}

public class SaveXraySettingsResponse
{
    public bool Success { get; set; }
}
