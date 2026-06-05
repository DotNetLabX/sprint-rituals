namespace Fokus.API.Features.Auth.RevokeInvitation;

[HttpDelete("/api/invitations/{id}")]
[Tags("Invitations")]
[Authorize(Roles = "Admin")]
public class RevokeInvitationEndpoint(InvitationRepository invitationRepository)
    : Endpoint<RevokeInvitationRequest>
{
    public override async Task HandleAsync(RevokeInvitationRequest req, CancellationToken ct)
    {
        var invitation = await invitationRepository.GetByIdAsync(req.Id, ct);
        if (invitation is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        invitation.Revoke();
        await invitationRepository.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
