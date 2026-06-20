using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.UpdateGroupTitle;

public sealed record UpdateGroupTitleRequest(int RetroSessionId, int GroupId, string? Title);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPatch("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}")]
[Tags("CardGroups")]
public sealed class UpdateGroupTitleEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<UpdateGroupTitleRequest, CardGroupResponse>
{
    public override async Task HandleAsync(UpdateGroupTitleRequest req, CancellationToken ct)
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

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.RenameGroup))
            ThrowError($"{RetroAction.RenameGroup} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        group.SetTitle(req.Title);

        await cardGroupRepository.SaveChangesAsync(ct);

        var groupCards = (await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct))
            .Where(c => c.GroupId == group.Id)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        await Send.OkAsync(group.Adapt<CardGroupResponse>() with
        {
            Cards = groupCards.Select(c => c.Adapt<CardGroupCardEntry>()).ToList(),
            IsFocused = session.FocusedGroupId == group.Id
        }, ct);
    }
}


