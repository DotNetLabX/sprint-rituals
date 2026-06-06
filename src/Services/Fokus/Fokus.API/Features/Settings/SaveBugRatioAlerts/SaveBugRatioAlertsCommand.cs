namespace Fokus.API.Features.Settings.SaveBugRatioAlerts;

public class SaveBugRatioAlertsCommand
{
    public int BugRatioAlertThreshold { get; set; } = 50;
    public int BugRatioConsecutiveSprintCount { get; set; } = 2;
    public int DefaultSpPerBug { get; set; } = 3;
}

public class SaveBugRatioAlertsCommandValidator : Validator<SaveBugRatioAlertsCommand>
{
    public SaveBugRatioAlertsCommandValidator()
    {
        RuleFor(x => x.BugRatioAlertThreshold)
            .InclusiveBetween(0, 100)
            .WithMessage("Bug ratio alert threshold must be between 0 and 100.");

        RuleFor(x => x.BugRatioConsecutiveSprintCount)
            .InclusiveBetween(1, 10)
            .WithMessage("Bug ratio consecutive sprint count must be between 1 and 10.");

        RuleFor(x => x.DefaultSpPerBug)
            .InclusiveBetween(0, 13)
            .WithMessage("Default SP per bug must be between 0 and 13.");
    }
}

public class SaveBugRatioAlertsResponse
{
    public bool Success { get; set; }
}
