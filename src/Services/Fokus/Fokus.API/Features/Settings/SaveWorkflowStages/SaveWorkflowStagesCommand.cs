namespace Fokus.API.Features.Settings.SaveWorkflowStages;

public class SaveWorkflowStagesCommand
{
    public List<string> Stages { get; set; } = [];
}

public class SaveWorkflowStagesCommandValidator : Validator<SaveWorkflowStagesCommand>
{
    public SaveWorkflowStagesCommandValidator()
    {
        RuleForEach(x => x.Stages)
            .NotEmpty()
            .WithMessage("Stage must not be empty or blank.");
    }
}
