namespace Fokus.API.Features.Auth.ValidateInvitation;

[AllowAnonymous]
[HttpGet("/api/invitations/{token}/validate")]
[Tags("Invitations")]
public class ValidateInvitationEndpoint(InvitationRepository invitationRepository)
    : Endpoint<ValidateInvitationRequest, ValidateInvitationResponse>
{
    public override async Task HandleAsync(ValidateInvitationRequest req, CancellationToken ct)
    {
        var invitation = await invitationRepository.GetByTokenAsync(req.Token, ct);

        if (invitation is null)
        {
            await SendOkAsync(new ValidateInvitationResponse { Valid = false, Error = "not-found" }, ct);
            return;
        }

        if (invitation.Status == "Accepted")
        {
            await SendOkAsync(new ValidateInvitationResponse { Valid = false, Error = "used" }, ct);
            return;
        }

        if (invitation.Status == "Revoked")
        {
            await SendOkAsync(new ValidateInvitationResponse { Valid = false, Error = "revoked" }, ct);
            return;
        }

        if (invitation.IsExpired)
        {
            await SendOkAsync(new ValidateInvitationResponse { Valid = false, Error = "expired" }, ct);
            return;
        }

        await SendOkAsync(new ValidateInvitationResponse { Valid = true, Email = invitation.Email }, ct);
    }
}
