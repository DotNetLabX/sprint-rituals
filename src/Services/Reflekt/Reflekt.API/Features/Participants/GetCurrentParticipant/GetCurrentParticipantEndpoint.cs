using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.GetCurrentParticipant;

public sealed record GetCurrentParticipantRequest(int SprintId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpGet("/api/sprints/{sprintId}/participants/me")]
[Tags("Participants")]
public sealed class GetCurrentParticipantEndpoint(ParticipantRepository participantRepository)
    : Endpoint<GetCurrentParticipantRequest, ParticipantResponse>
{
    public override async Task HandleAsync(GetCurrentParticipantRequest request, CancellationToken ct)
    {
        var participantIdClaim = User.FindFirstValue("participantId");
        var sprintIdClaim = User.FindFirstValue("sprintId");

        if (!int.TryParse(sprintIdClaim, out var claimSprintId) || claimSprintId != request.SprintId)
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
