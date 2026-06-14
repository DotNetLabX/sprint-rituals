namespace Fokus.API.Features.Auth.RevokeInvitation;

[HttpDelete("/api/invitations/{id}")]
[Tags("Invitations")]
[Authorize(Roles = "Admin")]
public class RevokeInvitationEndpoint(InvitationRepository invitationRepository)
    : Endpoint<RevokeInvitationCommand>
{
    public override async Task HandleAsync(RevokeInvitationCommand req, CancellationToken ct)
    {
        var invitation = await invitationRepository.GetByIdAsync(req.Id, ct);
        if (invitation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        invitation.Revoke();
        try
        {
            await invitationRepository.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            await Send.ResponseAsync((object?)null, 409, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
