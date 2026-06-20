using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.FetchEvidenceLink;

public sealed class BroadcastEvidenceLinkFetchedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<EvidenceLinkFetchedEvent>
{
    public Task HandleAsync(EvidenceLinkFetchedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Card.RetroSessionId}")
            .SendAsync("EvidenceLinkFetched", ev.EvidenceLink.Adapt<EvidenceLinkResponse>(), ct);
}
