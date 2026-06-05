namespace Fokus.API.Features.Settings.SaveBugRatioAlerts;

public class SaveBugRatioAlertsRequest
{
    public int AlertThreshold { get; set; } = 50;
    public int ConsecutiveSprintCount { get; set; } = 2;
    public int DefaultSpPerBug { get; set; } = 3;
}

public class SaveBugRatioAlertsRequestValidator : Validator<SaveBugRatioAlertsRequest>
{
    public SaveBugRatioAlertsRequestValidator()
    {
        RuleFor(x => x.AlertThreshold)
            .InclusiveBetween(0, 100)
            .WithMessage("Bug ratio alert threshold must be between 0 and 100.");

        RuleFor(x => x.ConsecutiveSprintCount)
            .InclusiveBetween(1, 10)
            .WithMessage("Bug ratio consecutive sprint count must be between 1 and 10.");

        RuleFor(x => x.DefaultSpPerBug)
            .InclusiveBetween(0, 13)
            .WithMessage("Default SP per bug must be between 0 and 13.");
    }
}

[HttpPut("/api/settings/bug-ratio-alerts")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveBugRatioAlertsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveBugRatioAlertsRequest, SaveBugRatioAlertsResponse>
{
    public override async Task HandleAsync(SaveBugRatioAlertsRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.BugRatioAlertThreshold = req.AlertThreshold;
        existing.BugRatioConsecutiveSprintCount = req.ConsecutiveSprintCount;
        existing.DefaultSpPerBug = req.DefaultSpPerBug;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveBugRatioAlertsResponse { Success = true }, ct);
    }
}

public class SaveBugRatioAlertsResponse
{
    public bool Success { get; set; }
}
