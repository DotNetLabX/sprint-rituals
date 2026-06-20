using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.API.Features.Participants.JoinRetro;

public sealed class BroadcastParticipantJoinedHandler(IHubContext<RetroHub> hubContext)
    : IEventHandler<ParticipantJoinedEvent>
{
    public Task HandleAsync(ParticipantJoinedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"retro:{ev.Participant.RetroSessionId}")
            .SendAsync("ParticipantJoined", new { id = ev.Participant.Id, displayName = ev.Participant.DisplayName }, ct);
}
