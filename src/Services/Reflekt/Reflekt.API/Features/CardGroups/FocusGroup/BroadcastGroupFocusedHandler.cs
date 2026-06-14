using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.CardGroups.FocusGroup;

public sealed class BroadcastGroupFocusedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<GroupFocusedEvent>
{
    public Task HandleAsync(GroupFocusedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("GroupFocused", new
            {
                groupId = ev.GroupId,
                previousGroupId = ev.PreviousGroupId
            }, ct);
}
