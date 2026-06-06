namespace Fokus.API.Features.Settings.GetCycleTimeBoundaries;

public record GetCycleTimeBoundariesResponse(
    string StartStage,
    string EndStage,
    List<string> AvailableStages,
    int WorkflowStageCount);
