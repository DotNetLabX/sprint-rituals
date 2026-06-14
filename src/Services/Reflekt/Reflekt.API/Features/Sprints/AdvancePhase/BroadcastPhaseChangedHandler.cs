using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Sprints.Timer;
using Reflekt.API.Hubs;
using Reflekt.Domain.Sprints.Events;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed class BroadcastPhaseChangedHandler(IHubContext<SprintHub> hubContext, TimerService timerService)
    : IEventHandler<PhaseAdvancedEvent>
{
    public async Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
    {
        var sprintId = ev.Sprint.Id;
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("PhaseChanged", ev.Sprint.CurrentPhase.ToString(), ct);

        SprintHub.ClearReadiness(sprintId);
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("ReadinessReset", new { }, ct);

        timerService.ClearTimer(sprintId);

        var isTimedPhase = ev.Sprint.CurrentPhase is
            Reflekt.Domain.Sprints.SprintPhase.Writing or
            Reflekt.Domain.Sprints.SprintPhase.Voting or
            Reflekt.Domain.Sprints.SprintPhase.Discussing;

        if (isTimedPhase)
        {
            var durationSeconds = ev.Sprint.GetTimerDefaultForPhase() * 60;
            var snapshot = timerService.Start(sprintId, durationSeconds);
            await hubContext.Clients.Group($"sprint:{sprintId}")
                .SendAsync("TimerUpdated", new
                {
                    status = snapshot.Status.ToString().ToLowerInvariant(),
                    remainingSeconds = snapshot.RemainingSeconds,
                    endTimeUtc = snapshot.EndTimeUtc,
                }, ct);
        }
        else
        {
            await hubContext.Clients.Group($"sprint:{sprintId}")
                .SendAsync("TimerUpdated", new
                {
                    status = "stopped",
                    remainingSeconds = 0,
                    endTimeUtc = (DateTime?)null,
                }, ct);
        }
    }
}
