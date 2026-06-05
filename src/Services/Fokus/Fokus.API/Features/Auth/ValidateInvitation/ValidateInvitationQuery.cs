namespace Fokus.API.Features.Auth.ValidateInvitation;

public class ValidateInvitationRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ValidateInvitationResponse
{
    public bool Valid { get; set; }
    public string? Email { get; set; }
    public string? Error { get; set; }
}
