namespace Fokus.API.Features.Analytics.GetDeveloperQuality;

public class GetDeveloperQualityRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetDeveloperQualityRequestValidator : Validator<GetDeveloperQualityRequest>
{
    public GetDeveloperQualityRequestValidator()
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

public class DeveloperQualityResponse
{
    public bool HasQaData { get; set; }
    public List<DeveloperQualitySprintInfoResponse> Sprints { get; set; } = [];
    public List<DeveloperQualityEntryResponse> Developers { get; set; } = [];
}

public class DeveloperQualitySprintInfoResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class DeveloperQualitySprintBreakdownResponse
{
    public int SprintId { get; set; }
    public int Stories { get; set; }
    public int Covered { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal PassRatePercent { get; set; }
    public int Untested { get; set; }
    public int BugsFound { get; set; }
    public string? CoverageRag { get; set; }
    public string? PassRateRag { get; set; }
    public decimal? CoveragePercentDelta { get; set; }
    public string? CoveragePercentDeltaDirection { get; set; }
    public string? CoveragePercentDeltaPolarity { get; set; }
    public decimal? PassRatePercentDelta { get; set; }
    public string? PassRatePercentDeltaDirection { get; set; }
    public string? PassRatePercentDeltaPolarity { get; set; }
    public int? BugsFoundDelta { get; set; }
    public string? BugsFoundDeltaDirection { get; set; }
    public string? BugsFoundDeltaPolarity { get; set; }
    public List<SparklinePointResponse>? CoverageSparkline { get; set; }
    public List<SparklinePointResponse>? PassRateSparkline { get; set; }
}

public class SparklinePointResponse
{
    public string SprintName { get; set; } = "";
    public decimal Value { get; set; }
}

public class DeveloperQualityEntryResponse
{
    public string AccountId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? SubTeam { get; set; }
    public string? AvatarUrl { get; set; }
    public List<DeveloperQualitySprintBreakdownResponse> SprintBreakdowns { get; set; } = [];
    public int? BelowMedianStreak { get; set; }
}
