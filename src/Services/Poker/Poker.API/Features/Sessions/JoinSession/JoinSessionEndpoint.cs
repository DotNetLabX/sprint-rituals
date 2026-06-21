using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;

namespace Poker.API.Features.Sessions.JoinSession;

/// <summary>
/// POST /api/poker/sessions/{id}/participants — Anyone (Google or guest) joins a session.
/// Open-link join, no waiting room (Rule 20 — intentionally distinct from Reflekt).
/// Authenticated re-entry is idempotent (returns existing participant).
/// 200; 404 session not found; 409 session already closed.
/// </summary>
[AllowAnonymous]
[HttpPost("/api/poker/sessions/{id}/participants")]
[Tags("PokerSessions")]
public sealed class JoinSessionEndpoint(
    PlanningSessionRepository sessionRepository,
    SessionParticipantRepository participantRepository)
    : Endpoint<JoinSessionCommand, JoinSessionResponse>
{
    public override async Task HandleAsync(JoinSessionCommand cmd, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(cmd.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.Status == Poker.Domain.Planning.SessionStatus.Closed)
        {
            ThrowError("Session is closed.", StatusCodes.Status409Conflict);
            return;
        }

        var personId = User.GetPersonIdOrNull();

        // Authenticated re-entry: idempotent (Rule 20 / mirror JoinRetroEndpoint:27-35)
        if (personId is not null)
        {
            var existing = await participantRepository.GetByPersonIdInSessionAsync(cmd.Id, personId.Value, ct);
            if (existing is not null)
            {
                await Send.OkAsync(existing.ToJoinResponse(cmd.Id), ct);
                return;
            }
        }

        var participant = personId is not null
            ? JoinAuthenticated(session, personId.Value)
            : JoinAsGuest(session, cmd.DisplayName);

        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await Send.OkAsync(participant.ToJoinResponse(cmd.Id), ct);
    }

    private Poker.Domain.Planning.Participants.SessionParticipant JoinAuthenticated(
        PlanningSession session,
        int personId)
    {
        var displayName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(displayName))
            ThrowError("Your account has no display name.", StatusCodes.Status400BadRequest);

        return session.AddAuthenticatedParticipant(personId, displayName!, DateTime.UtcNow);
    }

    private Poker.Domain.Planning.Participants.SessionParticipant JoinAsGuest(
        PlanningSession session,
        string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            ThrowError("Display name is required for guest join.", StatusCodes.Status400BadRequest);

        return session.AddGuestParticipant(displayName!, DateTime.UtcNow);
    }
}
