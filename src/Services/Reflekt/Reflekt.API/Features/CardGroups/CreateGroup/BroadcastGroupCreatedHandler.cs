using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

public sealed class BroadcastGroupCreatedHandler(
    IHubContext<SprintHub> hubContext,
    CardRepository cardRepository)
    : IEventHandler<CardGroupCreatedEvent>
{
    public async Task HandleAsync(CardGroupCreatedEvent ev, CancellationToken ct)
    {
        var group = ev.Group;
        var cards = (await cardRepository.GetBySprintIdAsync(group.SprintId, ct))
            .Where(c => c.GroupId == group.Id)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        await hubContext.Clients.Group($"sprint:{group.SprintId}")
            .SendAsync("GroupCreated", new
            {
                group = new
                {
                    id = group.Id,
                    sprintId = group.SprintId,
                    columnType = group.ColumnType.ToString(),
                    title = group.Title,
                    cardIds = cards.Select(c => c.Id).ToList()
                }
            }, ct);
    }
}
