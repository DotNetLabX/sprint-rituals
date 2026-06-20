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

public sealed record CastVoteRequest(int RetroSessionId, int GroupId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class CastVoteEndpoint(
    RetroSessionRepository retroSessionRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<RetroHub> hubContext)
    : Endpoint<CastVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(CastVoteRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("retroSessionId"), out var claimRetroSessionId) || claimRetroSessionId != req.RetroSessionId)
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
        if (group is null || group.RetroSessionId != req.RetroSessionId) { await Send.NotFoundAsync(ct); return; }

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.CastVote))
            ThrowError($"{RetroAction.CastVote} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        await using var tx = await voteRepository.BeginSerializableAsync(ct);

        var usedVotes = await voteRepository.CountByParticipantAsync(participantId, ct);
        if (usedVotes >= session.VotesPerParticipant)
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

        await hubContext.Clients.Group($"retro:{req.RetroSessionId}")
            .SendAsync("VoteCast", new { groupId = req.GroupId, newCount = voteCount }, ct);

        await Send.ResponseAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), 201, ct);
    }
}


