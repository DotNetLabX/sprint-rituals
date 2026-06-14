using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.CloseSprint;

public sealed class BroadcastSprintClosedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<SprintClosedEvent>
{
    public Task HandleAsync(SprintClosedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("SprintClosed", new { sprintId = ev.Sprint.Id }, ct);
}
