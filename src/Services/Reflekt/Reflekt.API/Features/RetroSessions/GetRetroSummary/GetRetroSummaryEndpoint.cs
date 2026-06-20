using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.RetroSessions.GetRetroSummary;

public sealed record GetRetroSummaryRequest(int RetroSessionId);

[AllowAnonymous]
[HttpGet("/api/retro-sessions/{retroSessionId}/summary")]
[Tags("RetroSessions")]
public sealed class GetRetroSummaryEndpoint(RetroSessionRepository retroSessionRepository, ParticipantRepository participantRepository, TeamRepository teamRepository)
    : Endpoint<GetRetroSummaryRequest, RetroSessionSummaryResponse>
{
    public override async Task HandleAsync(GetRetroSummaryRequest request, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(request.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var participants = await participantRepository.GetByRetroSessionIdAsync(request.RetroSessionId, ct);
        var participantDtos = participants.Select(p => new RetroSessionSummaryParticipant(p.DisplayName)).ToList();

        // Owner comparison keys on the Identity Person id (Team.OwnerId is a Person id after RA2),
        // read from the person_id cookie claim â€” NOT NameIdentifier (the User id), which would
        // compare Person-id â†” User-id and silently set isScrumMaster=false for the real owner.
        var isScrumMaster = false;
        var personIdClaim = User.FindFirstValue("person_id");
        if (personIdClaim is not null && int.TryParse(personIdClaim, out var personId))
        {
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            isScrumMaster = team is not null && team.OwnerId == personId;
        }

        await Send.OkAsync(new RetroSessionSummaryResponse(
            session.Id,
            session.TeamId,
            session.Name,
            session.State.ToString(),
            session.CurrentPhase.ToString(),
            session.VotesPerParticipant,
            isScrumMaster,
            participantDtos,
            session.Column1.Name,
            session.Column1.Color.Value,
            session.Column2.Name,
            session.Column2.Color.Value,
            session.Column3.Name,
            session.Column3.Color.Value,
            session.Timers.Writing,
            session.Timers.Voting,
            session.Timers.Discussing), ct);
    }
}


