namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

public class GetDeveloperDetailQuery
{
    public string AccountId { get; set; } = string.Empty;
    public int? Last { get; set; }
}

public class GetDeveloperDetailQueryValidator : Validator<GetDeveloperDetailQuery>
{
    public GetDeveloperDetailQueryValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId is required.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be 0 or greater.");
    }
}
