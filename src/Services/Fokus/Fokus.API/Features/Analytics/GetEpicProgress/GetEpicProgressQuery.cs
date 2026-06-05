namespace Fokus.API.Features.Analytics.GetEpicProgress;

public class GetEpicProgressRequest
{
    public string? SubTeam { get; set; }
}

public class GetEpicProgressRequestValidator : Validator<GetEpicProgressRequest>
{
    public GetEpicProgressRequestValidator()
    {
        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}
