namespace Fokus.API.Features.Analytics.GetQaWorkload;

public class GetQaWorkloadRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetQaWorkloadRequestValidator : Validator<GetQaWorkloadRequest>
{
    public GetQaWorkloadRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be at least 1.");

        RuleFor(x => x)
            .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
            .WithMessage("SprintId and Last cannot both be provided.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

// --- Response DTOs ---

public class QaWorkloadResponseDto
{
    public bool HasQaData { get; set; }
    public string Mode { get; set; } = "multi";
    public QaWorkloadMultiSprintDto? MultiSprint { get; set; }
    public QaWorkloadSingleSprintDto? SingleSprint { get; set; }
}

// --- Multi-sprint DTOs ---

public class QaWorkloadSprintInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class QaWorkloadTeamMetricsDto
{
    public int TotalTes { get; set; }
    public int TotalRunsCompleted { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal TeamPassRate { get; set; }
}

public class QaWorkloadSprintBreakdownDto
{
    public int SprintId { get; set; }
    public string SprintName { get; set; } = "";
    public int TesOwned { get; set; }
    public int RunsCompleted { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
    public int StoriesCovered { get; set; }
    public int BugsFound { get; set; }
}

public class WorkloadAlertDto
{
    public bool IsActive { get; set; }
    public int ConsecutiveSprintCount { get; set; }
    public int ThresholdPercent { get; set; }
}

public class QaWorkloadEntryDto
{
    public string? AccountId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? SubTeam { get; set; }
    public string? AvatarUrl { get; set; }
    public int TesOwned { get; set; }
    public int RunsCompleted { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
    public int StoriesCovered { get; set; }
    public int BugsFound { get; set; }
    public List<QaWorkloadSprintBreakdownDto> SprintBreakdowns { get; set; } = [];
    public WorkloadAlertDto WorkloadAlert { get; set; } = new();
}

public class QaWorkloadMultiSprintDto
{
    public List<QaWorkloadSprintInfoDto> Sprints { get; set; } = [];
    public QaWorkloadTeamMetricsDto TeamMetrics { get; set; } = new();
    public List<QaWorkloadEntryDto> Developers { get; set; } = [];
}

// --- Single-sprint DTOs ---

public class SparklinePointDto
{
    public string SprintName { get; set; } = "";
    public decimal Value { get; set; }
}

public class QaWorkloadMetricCardDto
{
    public string Name { get; set; } = "";
    public decimal Value { get; set; }
    public string DisplayValue { get; set; } = "";
    public decimal? Delta { get; set; }
    public string? DeltaDirection { get; set; }
    public string? DeltaPolarity { get; set; }
    public List<SparklinePointDto> Sparkline { get; set; } = [];
    public string? Rag { get; set; }
}

public class QaWorkloadSingleTeamMetricsDto
{
    public QaWorkloadMetricCardDto TotalTes { get; set; } = new();
    public QaWorkloadMetricCardDto TotalRunsCompleted { get; set; } = new();
    public QaWorkloadMetricCardDto TeamPassRate { get; set; } = new();
}

public class QaWorkloadSingleEntryDto
{
    public string? AccountId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? SubTeam { get; set; }
    public string? AvatarUrl { get; set; }
    public int TesOwned { get; set; }
    public int RunsCompleted { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
    public int StoriesCovered { get; set; }
    public int BugsFound { get; set; }
    public decimal? TesOwnedDelta { get; set; }
    public string? TesOwnedDirection { get; set; }
    public string? TesOwnedPolarity { get; set; }
    public decimal? RunsCompletedDelta { get; set; }
    public string? RunsCompletedDirection { get; set; }
    public string? RunsCompletedPolarity { get; set; }
    public decimal? PassCountDelta { get; set; }
    public string? PassCountDirection { get; set; }
    public string? PassCountPolarity { get; set; }
    public decimal? FailCountDelta { get; set; }
    public string? FailCountDirection { get; set; }
    public string? FailCountPolarity { get; set; }
    public decimal? PassRateDelta { get; set; }
    public string? PassRateDirection { get; set; }
    public string? PassRatePolarity { get; set; }
    public decimal? StoriesCoveredDelta { get; set; }
    public string? StoriesCoveredDirection { get; set; }
    public string? StoriesCoveredPolarity { get; set; }
    public decimal? BugsFoundDelta { get; set; }
    public string? BugsFoundDirection { get; set; }
    public string? BugsFoundPolarity { get; set; }
    public WorkloadAlertDto WorkloadAlert { get; set; } = new();
}

public class QaWorkloadSingleSprintDto
{
    public QaWorkloadSprintInfoDto Sprint { get; set; } = new();
    public QaWorkloadSingleTeamMetricsDto TeamMetrics { get; set; } = new();
    public List<QaWorkloadSingleEntryDto> Developers { get; set; } = [];
}
