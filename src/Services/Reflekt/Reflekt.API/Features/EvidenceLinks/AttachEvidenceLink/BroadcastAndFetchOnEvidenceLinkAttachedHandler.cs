using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Cards;
using Reflekt.API.Hubs;
using Reflekt.API.OpenGraph;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink;

public sealed class BroadcastAndFetchOnEvidenceLinkAttachedHandler(
    ChannelWriter<FetchJob> channel,
    IHubContext<RetroHub> hubContext)
    : IEventHandler<EvidenceLinkAttachedEvent>
{
    public async Task HandleAsync(EvidenceLinkAttachedEvent ev, CancellationToken ct)
    {
        await hubContext.Clients.Group($"retro:{ev.Card.RetroSessionId}")
            .SendAsync("CardUpdated", ev.Card.Adapt<CardResponse>(), ct);

        if (ev.EvidenceLink.FetchStatus == FetchStatus.Pending)
            await channel.WriteAsync(new FetchJob(ev.Card.Id, ev.EvidenceLink.Id, ev.EvidenceLink.Url, ev.Card.RetroSessionId), ct);
    }
}
