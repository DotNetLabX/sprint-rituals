namespace Fokus.API.Features.Sprints.GetTestTimeline;

public class GetTestTimelineRequest
{
    public int SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetTestTimelineRequestValidator : Validator<GetTestTimelineRequest>
{
    public GetTestTimelineRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}
