using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.API.Features.Participants.KickParticipant;

public sealed class BroadcastParticipantKickedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<ParticipantKickedEvent>
{
    public Task HandleAsync(ParticipantKickedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Participant.RetroSessionId}")
            .SendAsync("ParticipantKicked", new { participantId = ev.Participant.Id }, ct);
}
