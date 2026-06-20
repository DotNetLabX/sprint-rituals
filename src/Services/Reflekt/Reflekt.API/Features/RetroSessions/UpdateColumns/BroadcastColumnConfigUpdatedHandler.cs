using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.RetroSessions.UpdateColumns;

public sealed class BroadcastColumnConfigUpdatedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<ColumnConfigUpdatedEvent>
{
    public Task HandleAsync(ColumnConfigUpdatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.RetroSession.Id}")
            .SendAsync("ColumnConfigUpdated", new
            {
                retroSessionId = ev.RetroSession.Id,
                column1Name = ev.RetroSession.Column1Name,
                column1Color = ev.RetroSession.Column1Color.Value,
                column2Name = ev.RetroSession.Column2Name,
                column2Color = ev.RetroSession.Column2Color.Value,
                column3Name = ev.RetroSession.Column3Name,
                column3Color = ev.RetroSession.Column3Color.Value,
            }, ct);
}

