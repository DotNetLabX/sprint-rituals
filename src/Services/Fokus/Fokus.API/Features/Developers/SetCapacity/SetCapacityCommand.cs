namespace Fokus.API.Features.Developers.SetCapacity;

public class SetCapacityCommand
{
    public string AccountId { get; set; } = string.Empty;
    public int SprintId { get; set; }
    public int CapacityPercent { get; set; }
}

public class SetCapacityResponse
{
    public string DeveloperAccountId { get; set; } = string.Empty;
    public int SprintId { get; set; }
    public int CapacityPercent { get; set; }
}

public class SetCapacityCommandValidator : Validator<SetCapacityCommand>
{
    public SetCapacityCommandValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.CapacityPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("Capacity percent must be between 0 and 100.");
    }
}
