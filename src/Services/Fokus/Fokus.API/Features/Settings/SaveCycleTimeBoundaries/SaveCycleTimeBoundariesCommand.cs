namespace Fokus.API.Features.Settings.SaveCycleTimeBoundaries;

public class SaveCycleTimeBoundariesCommand
{
    public string CycleTimeStartStage { get; set; } = string.Empty;
    public string CycleTimeEndStage { get; set; } = string.Empty;
}

public class SaveCycleTimeBoundariesCommandValidator : Validator<SaveCycleTimeBoundariesCommand>
{
    public SaveCycleTimeBoundariesCommandValidator()
    {
        RuleFor(x => x.CycleTimeStartStage)
            .NotEmpty()
            .WithMessage("CycleTimeStartStage is required.");

        RuleFor(x => x.CycleTimeEndStage)
            .NotEmpty()
            .WithMessage("CycleTimeEndStage is required.");
    }
}

public record SaveCycleTimeBoundariesResponse(string CycleTimeStartStage, string CycleTimeEndStage);
