namespace Fokus.API.Features.Settings.SaveExcludedStatuses;

public class SaveExcludedStatusesCommand
{
    public List<string> Statuses { get; set; } = [];
}

public class SaveExcludedStatusesCommandValidator : Validator<SaveExcludedStatusesCommand>
{
    public SaveExcludedStatusesCommandValidator()
    {
        RuleForEach(x => x.Statuses)
            .NotEmpty()
            .WithMessage("Status must not be empty or blank.");
    }
}
