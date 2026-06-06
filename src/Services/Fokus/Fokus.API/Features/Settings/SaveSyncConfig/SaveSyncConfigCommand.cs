namespace Fokus.API.Features.Settings.SaveSyncConfig;

public class SaveSyncConfigCommand
{
    public int SyncBackSprintCount { get; set; } = 20;
    public int PlanningWindowDays { get; set; } = 2;
}

public class SaveSyncConfigCommandValidator : Validator<SaveSyncConfigCommand>
{
    public SaveSyncConfigCommandValidator()
    {
        RuleFor(x => x.SyncBackSprintCount)
            .InclusiveBetween(1, 50)
            .WithMessage("Sync back sprint count must be between 1 and 50.");

        RuleFor(x => x.PlanningWindowDays)
            .InclusiveBetween(0, 7)
            .WithMessage("Planning window must be between 0 and 7 days.");
    }
}

public class SaveSyncConfigResponse
{
    public bool Success { get; set; }
}
