using Microsoft.AspNetCore.Authorization;
using Poker.Persistence.Repositories;

namespace Poker.API.Features.Sessions.GetSessionSummary;

/// <summary>
/// GET /api/poker/sessions/{id}/summary — Summary stats (Rule 17/18).
/// Readable by anyone with the link; no session-state restriction.
/// 200 + summary; 404 if session not found.
/// </summary>
[AllowAnonymous]
[HttpGet("/api/poker/sessions/{id}/summary")]
[Tags("PokerSessions")]
public sealed class GetSessionSummaryEndpoint(PlanningSessionRepository sessionRepository)
    : Endpoint<GetSessionSummaryQuery, SessionSummaryResponse>
{
    public override async Task HandleAsync(GetSessionSummaryQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(query.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        await Send.OkAsync(session.ToSummaryResponse(), ct);
    }
}
