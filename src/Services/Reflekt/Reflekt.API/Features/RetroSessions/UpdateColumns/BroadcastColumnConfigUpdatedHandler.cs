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
                // Payload key names are client-contract — must stay camelCase/unchanged.
                // Only the value source moves to the grouped domain path.
                column1Name = ev.RetroSession.Column1.Name,
                column1Color = ev.RetroSession.Column1.Color.Value,
                column2Name = ev.RetroSession.Column2.Name,
                column2Color = ev.RetroSession.Column2.Color.Value,
                column3Name = ev.RetroSession.Column3.Name,
                column3Color = ev.RetroSession.Column3.Color.Value,
            }, ct);
}
