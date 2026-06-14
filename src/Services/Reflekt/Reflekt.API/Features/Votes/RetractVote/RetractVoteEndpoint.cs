using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.RetractVote;

public sealed record RetractVoteRequest(int SprintId, int GroupId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpDelete("/api/sprints/{sprintId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class RetractVoteEndpoint(
    SprintRepository sprintRepository,
    VoteRepository voteRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<SprintHub> hubContext)
    : Endpoint<RetractVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(RetractVoteRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != req.SprintId)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(User.FindFirstValue("participantId"), out var participantId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.RetractVote))
            ThrowError($"{SprintAction.RetractVote} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        var vote = await voteRepository.GetOneByParticipantAndGroupAsync(participantId, req.GroupId, ct);
        if (vote is null)
            ThrowError("No votes to retract on this group.", StatusCodes.Status409Conflict);

        voteRepository.Remove(vote);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByGroupAsync(req.GroupId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndGroupAsync(participantId, req.GroupId, ct);

        await hubContext.Clients.Group($"sprint:{req.SprintId}")
            .SendAsync("VoteRetracted", new { groupId = req.GroupId, newCount = voteCount }, ct);

        await Send.OkAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), ct);
    }
}
