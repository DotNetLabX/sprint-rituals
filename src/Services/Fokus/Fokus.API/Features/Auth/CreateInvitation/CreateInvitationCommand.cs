using System.Security.Claims;

namespace Fokus.API.Features.Auth.CreateInvitation;

public class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class CreateInvitationResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string InviteLink { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CreateInvitationRequestValidator : Validator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.Role)
            .Must(r => r is null || r == "Admin" || r == "Manager")
            .WithMessage("Role must be 'Admin' or 'Manager'.")
            .When(x => x.Role is not null);
    }
}
