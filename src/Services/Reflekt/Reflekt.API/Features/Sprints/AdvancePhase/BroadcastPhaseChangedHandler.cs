using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.Sprints.Timer;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed class BroadcastPhaseChangedHandler(IHubContext<SprintHub> hubContext, TimerService timerService)
    : IEventHandler<PhaseAdvancedEvent>
{
    public async Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
    {
        var sprintId = ev.RetroSession.Id;
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("PhaseChanged", ev.RetroSession.CurrentPhase.ToString(), ct);

        SprintHub.ClearReadiness(sprintId);
        await hubContext.Clients.Group($"sprint:{sprintId}")
            .SendAsync("ReadinessReset", new { }, ct);

        timerService.ClearTimer(sprintId);

        var isTimedPhase = ev.RetroSession.CurrentPhase is
            Reflekt.Domain.RetroSessions.RetroPhase.Writing or
            Reflekt.Domain.RetroSessions.RetroPhase.Voting or
            Reflekt.Domain.RetroSessions.RetroPhase.Discussing;

        if (isTimedPhase)
        {
            var durationSeconds = ev.RetroSession.GetTimerDefaultForPhase() * 60;
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

