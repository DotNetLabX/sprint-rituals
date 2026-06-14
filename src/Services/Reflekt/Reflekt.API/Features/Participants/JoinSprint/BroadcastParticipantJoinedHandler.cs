using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Participants.Events;

namespace Reflekt.API.Features.Participants.JoinSprint;

public sealed class BroadcastParticipantJoinedHandler(IHubContext<SprintHub> hubContext)
    : IEventHandler<ParticipantJoinedEvent>
{
    public Task HandleAsync(ParticipantJoinedEvent ev, CancellationToken ct)
        => hubContext.Clients.Group($"sprint:{ev.Participant.SprintId}")
            .SendAsync("ParticipantJoined", new { id = ev.Participant.Id, displayName = ev.Participant.DisplayName }, ct);
}
