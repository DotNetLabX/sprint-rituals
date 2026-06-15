using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Votes;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.CastVote;

public sealed record CastVoteRequest(int SprintId, int GroupId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class CastVoteEndpoint(
    RetroSessionRepository retroSessionRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<SprintHub> hubContext)
    : Endpoint<CastVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(CastVoteRequest req, CancellationToken ct)
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

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId) { await Send.NotFoundAsync(ct); return; }

        var sprint = await retroSessionRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State == RetroSessionState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, RetroAction.CastVote))
            ThrowError($"{RetroAction.CastVote} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        await using var tx = await voteRepository.BeginSerializableAsync(ct);

        var usedVotes = await voteRepository.CountByParticipantAsync(participantId, ct);
        if (usedVotes >= sprint.VotesPerParticipant)
            ThrowError("Vote budget exhausted.", StatusCodes.Status409Conflict);

        var existingVote = await voteRepository.GetOneByParticipantAndGroupAsync(participantId, req.GroupId, ct);
        if (existingVote is not null)
            ThrowError("Already voted on this group.", StatusCodes.Status409Conflict);

        var vote = Vote.Create(req.GroupId, participantId);
        vote.ClearDomainEvents();
        await voteRepository.AddAsync(vote, ct);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByGroupAsync(req.GroupId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndGroupAsync(participantId, req.GroupId, ct);

        await tx.CommitAsync(ct);

        await hubContext.Clients.Group($"sprint:{req.SprintId}")
            .SendAsync("VoteCast", new { groupId = req.GroupId, newCount = voteCount }, ct);

        await Send.ResponseAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), 201, ct);
    }
}


