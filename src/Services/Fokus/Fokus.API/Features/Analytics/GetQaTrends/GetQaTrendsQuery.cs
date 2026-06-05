namespace Fokus.API.Features.Analytics.GetQaTrends;

public class GetQaTrendsRequest
{
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetQaTrendsRequestValidator : Validator<GetQaTrendsRequest>
{
    public GetQaTrendsRequestValidator()
    {
        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be 0 (all) or a positive number.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}
