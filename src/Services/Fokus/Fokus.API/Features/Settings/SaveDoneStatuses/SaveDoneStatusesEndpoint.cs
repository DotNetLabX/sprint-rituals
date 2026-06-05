namespace Fokus.API.Features.Settings.SaveDoneStatuses;

public class SaveDoneStatusesRequest
{
    public List<string> Statuses { get; set; } = [];
}

public class SaveDoneStatusesRequestValidator : Validator<SaveDoneStatusesRequest>
{
    public SaveDoneStatusesRequestValidator()
    {
        RuleFor(x => x.Statuses)
            .NotEmpty()
            .WithMessage("At least one done status is required.");

        RuleForEach(x => x.Statuses)
            .NotEmpty()
            .WithMessage("Status must not be empty or blank.");
    }
}

[HttpPut("/api/settings/done-statuses")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveDoneStatusesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveDoneStatusesRequest, List<string>>
{
    public override async Task HandleAsync(SaveDoneStatusesRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.DoneStatuses = req.Statuses;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(existing.DoneStatuses, ct);
    }
}
