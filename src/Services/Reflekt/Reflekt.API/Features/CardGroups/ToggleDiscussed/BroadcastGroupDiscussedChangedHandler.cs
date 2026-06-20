using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.API.Features.CardGroups.ToggleDiscussed;

public sealed class BroadcastGroupDiscussedChangedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<GroupDiscussedChangedEvent>
{
    public Task HandleAsync(GroupDiscussedChangedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Group.RetroSessionId}")
            .SendAsync("GroupDiscussedChanged", new
            {
                groupId = ev.Group.Id,
                isDiscussed = ev.Group.DiscussedAt.HasValue,
                discussedAt = ev.Group.DiscussedAt
            }, ct);
}
