using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Sessions.CloseSession;

/// <summary>
/// POST /api/poker/sessions/{id}/close — SM closes the session (Rule 14).
/// 200 + summary; 403 non-SM; 409 already closed.
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/poker/sessions/{id}/close")]
[Tags("PokerSessions")]
public sealed class CloseSessionEndpoint(PlanningSessionRepository sessionRepository)
    : Endpoint<CloseSessionCommand, SessionSummaryResponse>
{
    public override async Task HandleAsync(CloseSessionCommand cmd, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(cmd.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        // SM gate: caller must be the session creator
        var personId = User.GetPersonId();
        if (session.CreatedByPersonId != personId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (session.Status == Poker.Domain.Planning.SessionStatus.Closed)
        {
            ThrowError("Session is already closed.", StatusCodes.Status409Conflict);
            return;
        }

        session.Close(DateTime.UtcNow);

        await sessionRepository.SaveChangesAsync(ct);
        await Send.OkAsync(session.ToSummaryResponse(), ct);
    }
}
