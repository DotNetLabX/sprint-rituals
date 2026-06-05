namespace Fokus.API.Features.Settings.SaveAnalyticsTargets;

public class SaveAnalyticsTargetsRequest
{
    public decimal BugRatioTarget { get; set; } = 30;
}

public class SaveAnalyticsTargetsRequestValidator : Validator<SaveAnalyticsTargetsRequest>
{
    public SaveAnalyticsTargetsRequestValidator()
    {
        RuleFor(x => x.BugRatioTarget)
            .InclusiveBetween(0, 100)
            .WithMessage("Bug ratio target must be between 0 and 100.");
    }
}

[HttpPut("/api/settings/analytics-targets")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveAnalyticsTargetsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveAnalyticsTargetsRequest, SaveAnalyticsTargetsResponse>
{
    public override async Task HandleAsync(SaveAnalyticsTargetsRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.BugRatioTarget = req.BugRatioTarget;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveAnalyticsTargetsResponse { Success = true }, ct);
    }
}

public class SaveAnalyticsTargetsResponse
{
    public bool Success { get; set; }
}
