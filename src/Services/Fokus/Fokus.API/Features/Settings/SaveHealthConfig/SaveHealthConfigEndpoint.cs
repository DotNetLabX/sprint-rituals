namespace Fokus.API.Features.Settings.SaveHealthConfig;

public class SaveHealthConfigRequest
{
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
    public QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();
    public int QualityHealthWeight { get; set; } = 20;
    public QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();
}

public class SaveHealthConfigRequestValidator : Validator<SaveHealthConfigRequest>
{
    public SaveHealthConfigRequestValidator()
    {
        // Delivery weights unchanged
        RuleFor(x => x.HealthWeights)
            .Must(w => w.Completion + w.Disruption + w.CarryOver == 100)
            .WithMessage("Health weights must sum to 100.");

        RuleFor(x => x.HealthThresholds.CompletionGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.CompletionAmber)
            .InclusiveBetween(0, 100)
            .LessThan(x => x.HealthThresholds.CompletionGreen)
            .WithMessage("Completion amber threshold must be less than green.");

        RuleFor(x => x.HealthThresholds.DisruptionGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.DisruptionAmber)
            .InclusiveBetween(0, 100)
            .GreaterThan(x => x.HealthThresholds.DisruptionGreen)
            .WithMessage("Disruption amber threshold must be greater than green.");

        RuleFor(x => x.HealthThresholds.CarryOverGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.CarryOverAmber)
            .InclusiveBetween(0, 100)
            .GreaterThan(x => x.HealthThresholds.CarryOverGreen)
            .WithMessage("Carry-over amber threshold must be greater than green.");

        // QA health weight
        RuleFor(x => x.QualityHealthWeight)
            .InclusiveBetween(0, 100)
            .WithMessage("Quality health weight must be between 0 and 100.");

        // QA thresholds — all higher-is-better so green > amber
        RuleFor(x => x.QaHealthThresholds.CoverageGreen)
            .InclusiveBetween(0, 100);
        RuleFor(x => x.QaHealthThresholds.CoverageAmber)
            .InclusiveBetween(0, 100)
            .LessThan(x => x.QaHealthThresholds.CoverageGreen)
            .WithMessage("Coverage amber threshold must be less than green.");

        RuleFor(x => x.QaHealthThresholds.ExecutionGreen)
            .InclusiveBetween(0, 100);
        RuleFor(x => x.QaHealthThresholds.ExecutionAmber)
            .InclusiveBetween(0, 100)
            .LessThan(x => x.QaHealthThresholds.ExecutionGreen)
            .WithMessage("Execution rate amber threshold must be less than green.");

        RuleFor(x => x.QaHealthThresholds.PassRateGreen)
            .InclusiveBetween(0, 100);
        RuleFor(x => x.QaHealthThresholds.PassRateAmber)
            .InclusiveBetween(0, 100)
            .LessThan(x => x.QaHealthThresholds.PassRateGreen)
            .WithMessage("Pass rate amber threshold must be less than green.");

        // QA sub-score weights — minimum 1 each (BR14)
        RuleFor(x => x.QualitySubScoreWeights.CoverageWeight)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Coverage weight must be at least 1.");
        RuleFor(x => x.QualitySubScoreWeights.PassRateWeight)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Pass rate weight must be at least 1.");
    }
}

[HttpPut("/api/settings/health-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveHealthConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveHealthConfigRequest, SaveHealthConfigResponse>
{
    public override async Task HandleAsync(SaveHealthConfigRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.HealthThresholds = req.HealthThresholds;
        existing.HealthWeights = req.HealthWeights;
        existing.QaHealthThresholds = req.QaHealthThresholds;
        existing.QualityHealthWeight = req.QualityHealthWeight;
        existing.QualitySubScoreWeights = req.QualitySubScoreWeights;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveHealthConfigResponse { Success = true }, ct);
    }
}

public class SaveHealthConfigResponse
{
    public bool Success { get; set; }
}
