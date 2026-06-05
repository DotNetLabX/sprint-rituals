namespace Fokus.API.Features.Settings.SaveExcludedStatuses;

public class SaveExcludedStatusesRequest
{
    public List<string> Statuses { get; set; } = [];
}

public class SaveExcludedStatusesRequestValidator : Validator<SaveExcludedStatusesRequest>
{
    public SaveExcludedStatusesRequestValidator()
    {
        RuleForEach(x => x.Statuses)
            .NotEmpty()
            .WithMessage("Status must not be empty or blank.");
    }
}

[HttpPut("/api/settings/excluded-statuses")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveExcludedStatusesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveExcludedStatusesRequest, List<string>>
{
    public override async Task HandleAsync(SaveExcludedStatusesRequest req, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);
        settings.ExcludedFromScopeStatuses = req.Statuses;
        await repository.SaveAsync(settings, ct);
        await SendOkAsync(settings.ExcludedFromScopeStatuses, ct);
    }
}
