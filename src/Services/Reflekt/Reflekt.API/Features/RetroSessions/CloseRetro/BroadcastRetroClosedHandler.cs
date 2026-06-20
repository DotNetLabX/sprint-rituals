using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.RetroSessions.CloseRetro;

public sealed class BroadcastRetroClosedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<RetroSessionClosedEvent>
{
    public Task HandleAsync(RetroSessionClosedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.RetroSession.Id}")
            .SendAsync("RetroClosed", new { retroSessionId = ev.RetroSession.Id }, ct);
}

