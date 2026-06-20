using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.GetParticipants;

public sealed record GetParticipantsRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/retro-sessions/{retroSessionId}/participants")]
[Tags("Participants")]
public sealed class GetParticipantsEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    ParticipantRepository participantRepository)
    : Endpoint<GetParticipantsRequest, List<ParticipantResponse>>
{
    public override async Task HandleAsync(GetParticipantsRequest request, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(request.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var isGuestToken = User.FindFirstValue("participantId") is not null;

        if (isGuestToken)
        {
            if (!int.TryParse(User.FindFirstValue("retroSessionId"), out var claimRetroSessionId) || claimRetroSessionId != request.RetroSessionId)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var userId = User.GetPersonId();
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var participants = await participantRepository.GetByRetroSessionIdAsync(request.RetroSessionId, ct);
        await Send.OkAsync(participants.Select(p => p.Adapt<ParticipantResponse>()).ToList(), ct);
    }
}


