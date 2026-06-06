namespace Fokus.API.Features.Analytics.GetSprintSummary;

public class GetSprintSummaryQuery
{
    public int? SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetSprintSummaryQueryValidator : Validator<GetSprintSummaryQuery>
{
    public GetSprintSummaryQueryValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}
