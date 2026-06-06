namespace Fokus.API.Features.Settings.GetCycleTimeBoundaries;

[HttpGet("/api/settings/cycle-time-boundaries")]
[Tags("Settings")]
public class GetCycleTimeBoundariesEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<GetCycleTimeBoundariesResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        var availableStages = new List<string>();
        availableStages.AddRange(settings.WorkflowStages);
        availableStages.AddRange(settings.DoneStatuses);

        // BR13: start = second workflow stage (index 1), end = first done status
        var startStage = settings.CycleTimeStartStage
            ?? (settings.WorkflowStages.Count >= 2 ? settings.WorkflowStages[1] : settings.WorkflowStages.FirstOrDefault() ?? string.Empty);

        var endStage = settings.CycleTimeEndStage
            ?? (settings.DoneStatuses.Count > 0 ? settings.DoneStatuses[0] : string.Empty);

        await Send.OkAsync(new GetCycleTimeBoundariesResponse(startStage, endStage, availableStages, settings.WorkflowStages.Count), ct);
    }
}
