namespace Identity.API.Features.Auth.UpdateStatus;

public class UpdateStatusCommand
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateStatusCommandValidator : Validator<UpdateStatusCommand>
{
    public UpdateStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0.");
    }
}
