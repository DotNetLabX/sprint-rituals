namespace Fokus.API.Features.Settings.SaveCycleTimeBoundaries;

[HttpPut("/api/settings/cycle-time-boundaries")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveCycleTimeBoundariesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveCycleTimeBoundariesCommand, SaveCycleTimeBoundariesResponse>
{
    public override async Task HandleAsync(SaveCycleTimeBoundariesCommand req, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        // Build ordered stage list: workflow stages + done statuses
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startIndex = orderedStages.IndexOf(req.CycleTimeStartStage);
        var endIndex = orderedStages.IndexOf(req.CycleTimeEndStage);

        if (startIndex < 0)
        {
            AddError(r => r.CycleTimeStartStage, "CycleTimeStartStage does not match any configured workflow stage or done status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (endIndex < 0)
        {
            AddError(r => r.CycleTimeEndStage, "CycleTimeEndStage does not match any configured workflow stage or done status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (startIndex >= endIndex)
        {
            AddError(r => r.CycleTimeStartStage, "CycleTimeStartStage must precede CycleTimeEndStage in the configured order.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        settings.CycleTimeStartStage = req.CycleTimeStartStage;
        settings.CycleTimeEndStage = req.CycleTimeEndStage;
        await repository.SaveAsync(settings, ct);

        await Send.OkAsync(new SaveCycleTimeBoundariesResponse(StartStage: settings.CycleTimeStartStage, EndStage: settings.CycleTimeEndStage), ct);
    }
}
