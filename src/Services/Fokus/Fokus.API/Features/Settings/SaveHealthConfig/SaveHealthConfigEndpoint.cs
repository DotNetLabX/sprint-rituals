namespace Fokus.API.Features.Settings.SaveHealthConfig;

[HttpPut("/api/settings/health-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveHealthConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveHealthConfigCommand, SaveHealthConfigResponse>
{
    public override async Task HandleAsync(SaveHealthConfigCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        // Explicit assignment for complex reference types (HealthThresholdConfig etc. are ValueObjects).
        // Mapster deep-copy is avoided here because Pass 3 will introduce ComplexProperty owned-entity
        // configuration, and reference-swap semantics must be preserved for EF change-tracking.
        existing.HealthThresholds = req.HealthThresholds;
        existing.HealthWeights = req.HealthWeights;
        existing.QaHealthThresholds = req.QaHealthThresholds;
        existing.QualityHealthWeight = req.QualityHealthWeight;
        existing.QualitySubScoreWeights = req.QualitySubScoreWeights;
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveHealthConfigResponse { Success = true }, ct);
    }
}
