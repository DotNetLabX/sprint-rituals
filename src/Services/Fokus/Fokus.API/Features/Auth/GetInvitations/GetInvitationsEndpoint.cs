namespace Fokus.API.Features.Auth.GetInvitations;

[HttpGet("/api/invitations")]
[Tags("Invitations")]
[Authorize(Roles = "Admin")]
public class GetInvitationsEndpoint(InvitationRepository invitationRepository)
    : EndpointWithoutRequest<List<InvitationResponse>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var invitations = await invitationRepository.GetAllAsync(ct);

        var result = invitations.Select(i => new InvitationResponse
        {
            Id = i.Id,
            Email = i.Email,
            Role = i.Role,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt
        }).ToList();

        await SendOkAsync(result, ct);
    }
}
