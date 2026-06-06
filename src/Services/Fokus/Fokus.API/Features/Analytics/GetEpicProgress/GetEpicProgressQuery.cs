namespace Fokus.API.Features.Analytics.GetEpicProgress;

public class GetEpicProgressQuery
{
    public string? SubTeam { get; set; }
}

public class GetEpicProgressQueryValidator : Validator<GetEpicProgressQuery>
{
    public GetEpicProgressQueryValidator()
    {
        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}
