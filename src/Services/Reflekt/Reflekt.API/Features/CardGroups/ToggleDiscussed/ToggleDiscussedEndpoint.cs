using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.ToggleDiscussed;

public sealed record ToggleDiscussedRequest(int RetroSessionId, int GroupId);

public sealed record ToggleDiscussedResponse(int GroupId, bool IsDiscussed, DateTime? DiscussedAt);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}/toggle-discussed")]
[Tags("CardGroups")]
public sealed class ToggleDiscussedEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache)
    : Endpoint<ToggleDiscussedRequest, ToggleDiscussedResponse>
{
    public override async Task HandleAsync(ToggleDiscussedRequest req, CancellationToken ct)
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

        if (session.State != RetroSessionState.Active)
        {
            AddError("Retro must be Active.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.ToggleDiscussed))
            ThrowError($"{RetroAction.ToggleDiscussed} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        if (group.DiscussedAt.HasValue)
            group.UnmarkDiscussed();
        else
            group.MarkDiscussed();

        await cardGroupRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new ToggleDiscussedResponse(group.Id, group.DiscussedAt.HasValue, group.DiscussedAt), ct);
    }
}


