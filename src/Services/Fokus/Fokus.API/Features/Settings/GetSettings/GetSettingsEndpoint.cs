namespace Fokus.API.Features.Settings.GetSettings;

[HttpGet("/api/settings")]
[Tags("Settings")]
public class GetSettingsEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<GetSettingsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        await Send.OkAsync(new GetSettingsResponse
        {
            BoardId = settings.BoardId,
            DoneStatuses = settings.DoneStatuses,
            WorkflowStages = settings.WorkflowStages,
            HealthThresholds = settings.HealthThresholds,
            HealthWeights = settings.HealthWeights,
            BugRatioAlertThreshold = settings.BugRatio.AlertThreshold,
            BugRatioConsecutiveSprintCount = settings.BugRatio.ConsecutiveSprintCount,
            SyncBackSprintCount = settings.Sync.BackSprintCount,
            PlanningWindowDays = settings.Sync.PlanningWindowDays,
            DefaultSpPerBug = settings.BugRatio.DefaultSpPerBug,
            BugRatioTarget = settings.BugRatio.Target,
            XrayEnabled = settings.Xray.Enabled,
            XrayClientId = settings.Xray.ClientId,
            XrayClientSecret = MaskSecret(settings.Xray.ClientSecret),
            QaHealthThresholds = settings.QaHealthThresholds,
            QualityHealthWeight = settings.QualityHealthWeight,
            QualitySubScoreWeights = settings.QualitySubScoreWeights
        }, ct);
    }

    private static string? MaskSecret(string? secret) =>
        string.IsNullOrEmpty(secret) ? string.Empty : "****";
}
