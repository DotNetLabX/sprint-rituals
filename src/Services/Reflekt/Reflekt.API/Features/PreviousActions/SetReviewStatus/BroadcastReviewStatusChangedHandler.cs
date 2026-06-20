using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.PreviousActions.SetReviewStatus;

public sealed class BroadcastReviewStatusChangedHandler(
    IHubContext<RetroHub> hubContext,
    CardRepository cardRepository)
    : IEventHandler<ReviewStatusChangedEvent>
{
    public async Task HandleAsync(ReviewStatusChangedEvent ev, CancellationToken ct)
    {
        var card = ev.Card;
        var previousRetroSessionId = card.RetroSessionId;

        var totalCount = await cardRepository.CountActionItemsByRetroSessionIdAsync(previousRetroSessionId, ct);
        var unreviewedItems = await cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousRetroSessionId, ct);
        var reviewedCount = totalCount - unreviewedItems.Count;

        await hubContext.Clients
            .Group($"retro:{card.ReviewedInRetroSessionId}")
            .SendAsync("ReviewStatusChanged", new
            {
                cardId = card.Id,
                reviewStatus = card.ReviewStatus.ToString(),
                reviewedCount,
                totalCount,
            }, ct);
    }
}
