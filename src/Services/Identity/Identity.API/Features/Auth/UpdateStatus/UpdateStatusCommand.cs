namespace Identity.API.Features.Auth.UpdateStatus;

public class UpdateStatusRequest
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateStatusRequestValidator : Validator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0.");
    }
}
