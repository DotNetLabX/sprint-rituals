namespace Fokus.API.Features.Settings.SaveXraySettings;

public class SaveXraySettingsCommand
{
    public bool XrayEnabled { get; set; }
    public string? XrayClientId { get; set; }
    public string? XrayClientSecret { get; set; }
}

public class SaveXraySettingsCommandValidator : Validator<SaveXraySettingsCommand>
{
    public SaveXraySettingsCommandValidator()
    {
        RuleFor(x => x.XrayClientId)
            .NotEmpty()
            .WithMessage("Client ID is required when Xray is enabled.")
            .When(x => x.XrayEnabled);
    }
}

public class SaveXraySettingsResponse
{
    public bool Success { get; set; }
}
