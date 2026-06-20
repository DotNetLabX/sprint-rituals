using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.DissolveGroup;

public sealed record DissolveGroupRequest(int RetroSessionId, int GroupId);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}")]
[Tags("CardGroups")]
public sealed class DissolveGroupEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<RetroHub> hubContext)
    : Endpoint<DissolveGroupRequest>
{
    public override async Task HandleAsync(DissolveGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.UngroupCards))
            ThrowError($"{RetroAction.UngroupCards} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        var groupCards = (await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct))
            .Where(c => c.GroupId == req.GroupId)
            .ToList();

        if (groupCards.Count < 2)
            ThrowError("Cannot dissolve a solo group.", StatusCodes.Status400BadRequest);

        foreach (var card in groupCards)
        {
            var soloGroup = CardGroup.Create(req.RetroSessionId, card.ColumnType);
            soloGroup.ClearDomainEvents();
            await cardGroupRepository.AddAsync(soloGroup, ct);
            await cardGroupRepository.SaveChangesAsync(ct);
            card.SetGroupId(soloGroup.Id);
        }

        cardGroupRepository.Remove(group);
        await cardGroupRepository.SaveChangesAsync(ct);

        await hubContext.Clients.Group($"retro:{req.RetroSessionId}")
            .SendAsync("GroupDissolved", new { groupId = req.GroupId }, ct);

        await Send.NoContentAsync(ct);
    }
}


