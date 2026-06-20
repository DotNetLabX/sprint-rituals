using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.RetroSessions.Timer;
using Reflekt.API.Hubs;
using Reflekt.Domain.RetroSessions.Events;

namespace Reflekt.API.Features.RetroSessions.AdvancePhase;

public sealed class BroadcastPhaseChangedHandler(IHubContext<RetroHub> hubContext, TimerService timerService)
    : IEventHandler<PhaseAdvancedEvent>
{
    public async Task HandleAsync(PhaseAdvancedEvent ev, CancellationToken ct)
    {
        var retroSessionId = ev.RetroSession.Id;
        await hubContext.Clients.Group($"retro:{retroSessionId}")
            .SendAsync("PhaseChanged", ev.RetroSession.CurrentPhase.ToString(), ct);

        RetroHub.ClearReadiness(retroSessionId);
        await hubContext.Clients.Group($"retro:{retroSessionId}")
            .SendAsync("ReadinessReset", new { }, ct);

        timerService.ClearTimer(retroSessionId);

        var isTimedPhase = ev.RetroSession.CurrentPhase is
            Reflekt.Domain.RetroSessions.RetroPhase.Writing or
            Reflekt.Domain.RetroSessions.RetroPhase.Voting or
            Reflekt.Domain.RetroSessions.RetroPhase.Discussing;

        if (isTimedPhase)
        {
            var durationSeconds = ev.RetroSession.GetTimerDefaultForPhase() * 60;
            var snapshot = timerService.Start(retroSessionId, durationSeconds);
            await hubContext.Clients.Group($"retro:{retroSessionId}")
                .SendAsync("TimerUpdated", new
                {
                    status = snapshot.Status.ToString().ToLowerInvariant(),
                    remainingSeconds = snapshot.RemainingSeconds,
                    endTimeUtc = snapshot.EndTimeUtc,
                }, ct);
        }
        else
        {
            await hubContext.Clients.Group($"retro:{retroSessionId}")
                .SendAsync("TimerUpdated", new
                {
                    status = "stopped",
                    remainingSeconds = 0,
                    endTimeUtc = (DateTime?)null,
                }, ct);
        }
    }
}

