namespace Fokus.API.Features.Settings.SaveCycleTimeBoundaries;

public class SaveCycleTimeBoundariesRequest
{
    public string StartStage { get; set; } = string.Empty;
    public string EndStage { get; set; } = string.Empty;
}

public class SaveCycleTimeBoundariesRequestValidator : Validator<SaveCycleTimeBoundariesRequest>
{
    public SaveCycleTimeBoundariesRequestValidator()
    {
        RuleFor(x => x.StartStage)
            .NotEmpty()
            .WithMessage("StartStage is required.");

        RuleFor(x => x.EndStage)
            .NotEmpty()
            .WithMessage("EndStage is required.");
    }
}

public record SaveCycleTimeBoundariesResponse(string StartStage, string EndStage);

[HttpPut("/api/settings/cycle-time-boundaries")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveCycleTimeBoundariesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveCycleTimeBoundariesRequest, SaveCycleTimeBoundariesResponse>
{
    public override async Task HandleAsync(SaveCycleTimeBoundariesRequest req, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        // Build ordered stage list: workflow stages + done statuses
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startIndex = orderedStages.IndexOf(req.StartStage);
        var endIndex = orderedStages.IndexOf(req.EndStage);

        if (startIndex < 0)
        {
            AddError(r => r.StartStage, "StartStage does not match any configured workflow stage or done status.");
            await SendErrorsAsync(400, ct);
            return;
        }

        if (endIndex < 0)
        {
            AddError(r => r.EndStage, "EndStage does not match any configured workflow stage or done status.");
            await SendErrorsAsync(400, ct);
            return;
        }

        if (startIndex >= endIndex)
        {
            AddError(r => r.StartStage, "StartStage must precede EndStage in the configured order.");
            await SendErrorsAsync(400, ct);
            return;
        }

        settings.CycleTimeStartStage = req.StartStage;
        settings.CycleTimeEndStage = req.EndStage;
        await repository.SaveAsync(settings, ct);

        await SendOkAsync(new SaveCycleTimeBoundariesResponse(settings.CycleTimeStartStage, settings.CycleTimeEndStage), ct);
    }
}
