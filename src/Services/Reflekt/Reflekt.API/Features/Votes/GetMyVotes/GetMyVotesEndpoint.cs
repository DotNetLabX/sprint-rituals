using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Votes.GetMyVotes;

public sealed record GetMyVotesRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.Participant)]
[HttpGet("/api/retro-sessions/{retroSessionId}/votes/mine")]
[Tags("Votes")]
public sealed class GetMyVotesEndpoint(
    RetroSessionRepository retroSessionRepository,
    VoteRepository voteRepository)
    : Endpoint<GetMyVotesRequest, List<VoteDistributionEntry>>
{
    public override async Task HandleAsync(GetMyVotesRequest req, CancellationToken ct)
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

        var distribution = await voteRepository.GetVoteDistributionAsync(participantId, ct);
        await Send.OkAsync(distribution.Select(e => new VoteDistributionEntry(e.GroupId, e.Count)).ToList(), ct);
    }
}


