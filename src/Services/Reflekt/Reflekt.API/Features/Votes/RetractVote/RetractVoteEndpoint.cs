using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.RetractVote;

public sealed record RetractVoteRequest(int RetroSessionId, int GroupId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpDelete("/api/retro-sessions/{retroSessionId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class RetractVoteEndpoint(
    RetroSessionRepository retroSessionRepository,
    VoteRepository voteRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<RetroHub> hubContext)
    : Endpoint<RetractVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(RetractVoteRequest req, CancellationToken ct)
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

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.RetractVote))
            ThrowError($"{RetroAction.RetractVote} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        var vote = await voteRepository.GetOneByParticipantAndGroupAsync(participantId, req.GroupId, ct);
        if (vote is null)
            ThrowError("No votes to retract on this group.", StatusCodes.Status409Conflict);

        voteRepository.Remove(vote);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByGroupAsync(req.GroupId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndGroupAsync(participantId, req.GroupId, ct);

        await hubContext.Clients.Group($"retro:{req.RetroSessionId}")
            .SendAsync("VoteRetracted", new { groupId = req.GroupId, newCount = voteCount }, ct);

        await Send.OkAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), ct);
    }
}


