using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Votes.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.RetractVote;

public sealed class BroadcastVoteCountOnVoteRetractedHandler(
    IHubContext<RetroHub> hubContext,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository)
    : IEventHandler<VoteRetractedEvent>
{
    public async Task HandleAsync(VoteRetractedEvent ev, CancellationToken ct)
    {
        var groupId = ev.Vote.GroupId;
        var group = await cardGroupRepository.GetByIdAsync(groupId, ct);
        if (group is null) return;

        var newCount = await voteRepository.CountByGroupAsync(groupId, ct);
        await hubContext.Clients.Group($"retro:{group.RetroSessionId}")
            .SendAsync("VoteRetracted", new { groupId, newCount }, ct);
    }
}
