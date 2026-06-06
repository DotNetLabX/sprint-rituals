namespace Fokus.API.Features.Sprints.UpdateSprint;

public class UpdateSprintCommand
{
    public int SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Goal { get; set; }
}

public class UpdateSprintCommandValidator : Validator<UpdateSprintCommand>
{
    public UpdateSprintCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Sprint name is required.");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("Start date must be before end date.");
    }
}

public class UpdateSprintResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Goal { get; set; }
    public string State { get; set; } = string.Empty;
}
