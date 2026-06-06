namespace Fokus.API.Features.Settings.SaveAnalyticsTargets;

public class SaveAnalyticsTargetsCommand
{
    public decimal BugRatioTarget { get; set; } = 30;
}

public class SaveAnalyticsTargetsCommandValidator : Validator<SaveAnalyticsTargetsCommand>
{
    public SaveAnalyticsTargetsCommandValidator()
    {
        RuleFor(x => x.BugRatioTarget)
            .InclusiveBetween(0, 100)
            .WithMessage("Bug ratio target must be between 0 and 100.");
    }
}

public class SaveAnalyticsTargetsResponse
{
    public bool Success { get; set; }
}
