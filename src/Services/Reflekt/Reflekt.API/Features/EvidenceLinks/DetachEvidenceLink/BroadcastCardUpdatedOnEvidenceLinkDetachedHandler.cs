using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed class BroadcastCardUpdatedOnEvidenceLinkDetachedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<EvidenceLinkDetachedEvent>
{
    public Task HandleAsync(EvidenceLinkDetachedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Card.RetroSessionId}")
            .SendAsync("CardUpdated", ev.Card.Adapt<CardResponse>(), ct);
}
