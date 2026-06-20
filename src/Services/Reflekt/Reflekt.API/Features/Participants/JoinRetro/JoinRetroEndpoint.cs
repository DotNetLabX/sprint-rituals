using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.JoinRetro;

[AllowAnonymous]
[HttpPost("/api/retro-sessions/{retroSessionId}/join")]
[Tags("Participants")]
public sealed class JoinRetroEndpoint(RetroSessionRepository retroSessionRepository, ParticipantRepository participantRepository)
    : Endpoint<JoinRetroCommand, JoinRetroResponse>
{
    public override async Task HandleAsync(JoinRetroCommand command, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        // Suite members (Identity cookie) carry person_id; guests have no cookie → null (ADR-022).
        var personId = User.GetPersonIdOrNull();

        // Authenticated re-entry is idempotent and state-agnostic: a returning member (page reload, or
        // re-opening a closed retro's report) gets their existing participant back — mirroring guest
        // re-entry via the saved guest token. Identity is the Person, never the typed name.
        if (personId is not null)
        {
            var existing = await participantRepository.FindByPersonIdAsync(command.RetroSessionId, personId.Value, ct);
            if (existing is not null)
            {
                await Send.OkAsync(existing.Adapt<JoinRetroResponse>(), ct);
                return;
            }
        }

        // First-time join (either kind) requires an accepting retro.
        if (session.State != RetroSessionState.Active)
            ThrowError("Retro is not accepting participants.", StatusCodes.Status409Conflict);

        var participant = personId is not null
            ? CreateAuthenticatedParticipant(command.RetroSessionId, personId.Value)
            : await CreateGuestParticipantAsync(command, ct);

        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(participant.Adapt<JoinRetroResponse>(), 201, ct);
    }

    // Display name comes from the Identity cookie (ClaimTypes.Name), never the request body. No
    // display-name uniqueness gate — identity is the Person, so distinct members may share a name.
    // Seam (ADR-021): a future SM-approval gate slots in before this create.
    private Participant CreateAuthenticatedParticipant(int retroSessionId, int personId)
    {
        var displayName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(displayName))
            ThrowError("Your account has no display name.", StatusCodes.Status400BadRequest);

        return Participant.Join(displayName!, retroSessionId, personId);
    }

    // Guests have no stable identity: the display name is required from the body and must be unique per retro.
    private async Task<Participant> CreateGuestParticipantAsync(JoinRetroCommand command, CancellationToken ct)
    {
        var displayName = command.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
            ThrowError("Display name is required.", StatusCodes.Status400BadRequest);

        if (await participantRepository.ExistsByDisplayNameAsync(command.RetroSessionId, displayName!, ct))
            ThrowError("Name already taken.", StatusCodes.Status409Conflict);

        return Participant.Join(displayName!, command.RetroSessionId, personId: null);
    }
}
