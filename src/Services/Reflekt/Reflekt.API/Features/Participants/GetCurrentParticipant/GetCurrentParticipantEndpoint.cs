using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.GetCurrentParticipant;

public sealed record GetCurrentParticipantRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpGet("/api/retro-sessions/{retroSessionId}/participants/me")]
[Tags("Participants")]
public sealed class GetCurrentParticipantEndpoint(ParticipantRepository participantRepository)
    : Endpoint<GetCurrentParticipantRequest, ParticipantResponse>
{
    public override async Task HandleAsync(GetCurrentParticipantRequest request, CancellationToken ct)
    {
        var participantIdClaim = User.FindFirstValue("participantId");
        var retroSessionIdClaim = User.FindFirstValue("retroSessionId");

        if (!int.TryParse(retroSessionIdClaim, out var claimRetroSessionId) || claimRetroSessionId != request.RetroSessionId)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(participantIdClaim, out var participantId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var participant = await participantRepository.GetByIdAsync(participantId, ct);
        if (participant is null) { await Send.NotFoundAsync(ct); return; }

        await Send.OkAsync(participant.Adapt<ParticipantResponse>(), ct);
    }
}
