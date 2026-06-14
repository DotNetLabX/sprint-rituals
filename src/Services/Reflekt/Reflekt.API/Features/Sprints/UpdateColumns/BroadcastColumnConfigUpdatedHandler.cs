using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.UpdateColumns;

public sealed class BroadcastColumnConfigUpdatedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<ColumnConfigUpdatedEvent>
{
    public Task HandleAsync(ColumnConfigUpdatedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Sprint.Id}")
            .SendAsync("ColumnConfigUpdated", new
            {
                sprintId = ev.Sprint.Id,
                column1Name = ev.Sprint.Column1Name,
                column1Color = ev.Sprint.Column1Color.Value,
                column2Name = ev.Sprint.Column2Name,
                column2Color = ev.Sprint.Column2Color.Value,
                column3Name = ev.Sprint.Column3Name,
                column3Color = ev.Sprint.Column3Color.Value,
            }, ct);
}
