using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed class BroadcastCardUpdatedOnEvidenceLinkDetachedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<EvidenceLinkDetachedEvent>
{
    public Task HandleAsync(EvidenceLinkDetachedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Card.SprintId}")
            .SendAsync("CardUpdated", ev.Card.Adapt<CardResponse>(), ct);
}
