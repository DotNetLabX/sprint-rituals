using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Tickets.GetSessionTickets;

/// <summary>
/// GET /api/poker/sessions/{id}/tickets — Live Jira pass-through for tickets in the session's sprint.
/// SM only; 403 for non-SM; 409 for Free-mode sessions (Rule 12 — only Sprint-bound has tickets).
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpGet("/api/poker/sessions/{id}/tickets")]
[Tags("PokerSessions")]
public sealed class GetSessionTicketsEndpoint(
    PlanningSessionRepository sessionRepository,
    IPokerFokusService pokerFokusService)
    : Endpoint<GetSessionTicketsQuery, GetSessionTicketsResponse>
{
    public override async Task HandleAsync(GetSessionTicketsQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(query.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        // SM gate
        var personId = User.GetPersonId();
        if (session.CreatedByPersonId != personId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Rule 12: only Sprint-bound sessions have a ticket list
        if (session.Mode != Poker.Domain.Planning.SessionMode.SprintBound || session.SprintId is null)
        {
            ThrowError("Ticket list is only available for Sprint-bound sessions.", StatusCodes.Status409Conflict);
            return;
        }

        var result = await pokerFokusService.GetSprintTicketsAsync(
            new SprintTicketsQuery { SprintId = session.SprintId.Value }, ct);

        await Send.OkAsync(new GetSessionTicketsResponse(result.Tickets), ct);
    }
}

public record GetSessionTicketsResponse(IReadOnlyList<PokerTicket> Tickets);
