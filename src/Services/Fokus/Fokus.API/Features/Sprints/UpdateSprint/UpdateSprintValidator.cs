namespace Fokus.API.Features.Sprints.UpdateSprint;

public class UpdateSprintValidator : Validator<UpdateSprintRequest>
{
    public UpdateSprintValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Sprint name is required.");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("Start date must be before end date.");
    }
}
