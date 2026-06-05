namespace Fokus.API.Features.Settings.SaveSyncConfig;

public class SaveSyncConfigRequest
{
    public int SyncBackSprintCount { get; set; } = 20;
    public int PlanningWindowDays { get; set; } = 2;
}

public class SaveSyncConfigRequestValidator : Validator<SaveSyncConfigRequest>
{
    public SaveSyncConfigRequestValidator()
    {
        RuleFor(x => x.SyncBackSprintCount)
            .InclusiveBetween(1, 50)
            .WithMessage("Sync back sprint count must be between 1 and 50.");

        RuleFor(x => x.PlanningWindowDays)
            .InclusiveBetween(0, 7)
            .WithMessage("Planning window must be between 0 and 7 days.");
    }
}

[HttpPut("/api/settings/sync-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveSyncConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveSyncConfigRequest, SaveSyncConfigResponse>
{
    public override async Task HandleAsync(SaveSyncConfigRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.SyncBackSprintCount = req.SyncBackSprintCount;
        existing.PlanningWindowDays = req.PlanningWindowDays;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveSyncConfigResponse { Success = true }, ct);
    }
}

public class SaveSyncConfigResponse
{
    public bool Success { get; set; }
}
