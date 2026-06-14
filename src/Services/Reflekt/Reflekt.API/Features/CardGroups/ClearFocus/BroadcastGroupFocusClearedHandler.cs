using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.CardGroups.ClearFocus;

public sealed class BroadcastGroupFocusClearedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<GroupFocusClearedEvent>
{
    public Task HandleAsync(GroupFocusClearedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("GroupFocusCleared", new
            {
                previousGroupId = ev.PreviousGroupId
            }, ct);
}
