namespace Fokus.API.Features.Settings.SaveDoneStatuses;

public class SaveDoneStatusesCommand
{
    public List<string> Statuses { get; set; } = [];
}

public class SaveDoneStatusesCommandValidator : Validator<SaveDoneStatusesCommand>
{
    public SaveDoneStatusesCommandValidator()
    {
        RuleFor(x => x.Statuses)
            .NotEmpty()
            .WithMessage("At least one done status is required.");

        RuleForEach(x => x.Statuses)
            .NotEmpty()
            .WithMessage("Status must not be empty or blank.");
    }
}
