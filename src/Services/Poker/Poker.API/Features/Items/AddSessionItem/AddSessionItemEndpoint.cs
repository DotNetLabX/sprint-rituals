using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Items.AddSessionItem;

/// <summary>
/// POST /api/poker/sessions/{id}/items — SM adds a manual item (Free mode only, Rule 3).
/// 201; 403 non-SM; 409 Sprint-bound session (only free sessions accept manual items).
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/poker/sessions/{id}/items")]
[Tags("PokerSessions")]
public sealed class AddSessionItemEndpoint(PlanningSessionRepository sessionRepository)
    : Endpoint<AddSessionItemCommand, EstimationItemResponse>
{
    public override async Task HandleAsync(AddSessionItemCommand cmd, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdWithDetailsAsync(cmd.Id, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        // SM gate
        var personId = User.GetPersonId();
        if (session.CreatedByPersonId != personId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Rule 3: only Free sessions accept manually added items
        if (session.Mode == Poker.Domain.Planning.SessionMode.SprintBound)
        {
            ThrowError("Manual items can only be added to Free-mode sessions.", StatusCodes.Status409Conflict);
            return;
        }

        var order = session.Items.Count;
        var item = session.AddFreeItem(cmd.Label, order);

        await sessionRepository.SaveChangesAsync(ct);
        await Send.ResponseAsync(item.ToResponse(), StatusCodes.Status201Created, ct);
    }
}
