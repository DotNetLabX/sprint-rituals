using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.Sprints.CloseSprint;

public sealed class BroadcastSprintClosedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<RetroSessionClosedEvent>
{
    public Task HandleAsync(RetroSessionClosedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.RetroSession.Id}")
            .SendAsync("SprintClosed", new { sprintId = ev.RetroSession.Id }, ct);
}

