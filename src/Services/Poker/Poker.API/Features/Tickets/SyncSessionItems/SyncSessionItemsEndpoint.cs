using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.API.Features.Sessions;
using Poker.Persistence.Repositories;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Tickets.SyncSessionItems;

/// <summary>
/// POST /api/poker/sessions/{id}/tickets/sync — SM loads (or refreshes) the sprint's Jira tickets as
/// estimation items (Flow 3). Idempotent per ticket and never removes items (Rule 19).
/// 200 + full session; 403 non-SM; 409 Free-mode session.
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/poker/sessions/{id}/tickets/sync")]
[Tags("PokerSessions")]
public sealed class SyncSessionItemsEndpoint(
    PlanningSessionRepository sessionRepository,
    IPokerFokusService pokerFokusService)
    : Endpoint<SyncSessionItemsCommand, SessionResponse>
{
    public override async Task HandleAsync(SyncSessionItemsCommand cmd, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(cmd.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.CreatedByPersonId != User.GetPersonId())
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (session.Mode != Poker.Domain.Planning.SessionMode.SprintBound || session.SprintId is null)
        {
            ThrowError("Ticket items are only available for Sprint-bound sessions.", StatusCodes.Status409Conflict);
            return;
        }

        var result = await pokerFokusService.GetSprintTicketsAsync(
            new SprintTicketsQuery { SprintId = session.SprintId.Value }, ct);

        foreach (var ticket in result.Tickets)
            session.AddSprintBoundItem(ticket.Key, ticket.Summary, session.Items.Count);

        await sessionRepository.SaveChangesAsync(ct);
        await Send.OkAsync(session.ToResponse(), ct);
    }
}
