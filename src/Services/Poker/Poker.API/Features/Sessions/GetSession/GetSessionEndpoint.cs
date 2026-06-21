using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;

namespace Poker.API.Features.Sessions.GetSession;

/// <summary>
/// GET /api/poker/sessions/{id} — Returns full session state (mode, status, items, participants).
/// Anyone with the room link can read session state. 404 if not found.
/// </summary>
[AllowAnonymous]
[HttpGet("/api/poker/sessions/{id}")]
[Tags("PokerSessions")]
public sealed class GetSessionEndpoint(PlanningSessionRepository sessionRepository)
    : Endpoint<GetSessionQuery, SessionResponse>
{
    public override async Task HandleAsync(GetSessionQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(query.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        await Send.OkAsync(session.ToResponse(), ct);
    }
}
