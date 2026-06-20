using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.API.Features.RetroSessions.Timer;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Hubs;

[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]
public class RetroHub(
    ILogger<RetroHub> logger,
    TimerService timerService,
    IServiceScopeFactory scopeFactory) : Hub
{
    // retroSessionId -> participantId -> displayName (ready participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> ReadinessState = new();

    // retroSessionId -> connectionId -> participantId (all connected participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> ConnectionState = new();

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to RetroHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task JoinRetro(int retroSessionId)
    {
        var claimRetroSessionId = Context.User?.FindFirstValue("retroSessionId");
        if (!int.TryParse(claimRetroSessionId, out var parsedRetroSessionId) || parsedRetroSessionId != retroSessionId)
            throw new HubException("Not authorized to join this retro.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"retro:{retroSessionId}");

        // Track connection
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        if (int.TryParse(participantIdClaim, out var participantId))
        {
            var connections = ConnectionState.GetOrAdd(retroSessionId, _ => new ConcurrentDictionary<string, int>());
            connections[Context.ConnectionId] = participantId;
        }

        // Send readiness snapshot to joining client
        var readyDict = ReadinessState.GetOrAdd(retroSessionId, _ => new ConcurrentDictionary<int, string>());
        var connections2 = ConnectionState.GetOrAdd(retroSessionId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections2.Values.Distinct().Count();

        await Clients.Caller.SendAsync("ReadinessSnapshot", new
        {
            readyParticipantIds = readyDict.Keys.ToList(),
            readyCount = readyDict.Count,
            totalCount,
        });

        // Auto-start timer if timed phase and timer not yet started
        var timerSnapshot = timerService.GetSnapshot(retroSessionId);
        if (timerSnapshot.Status == TimerStatus.Stopped)
        {
            using var scope = scopeFactory.CreateScope();
            var retroSessionRepo = scope.ServiceProvider.GetRequiredService<RetroSessionRepository>();
            var session = await retroSessionRepo.GetByIdAsync(retroSessionId, Context.ConnectionAborted);
            if (session is not null && session.CurrentPhase is
                Reflekt.Domain.RetroSessions.RetroPhase.Writing or
                Reflekt.Domain.RetroSessions.RetroPhase.Voting or
                Reflekt.Domain.RetroSessions.RetroPhase.Discussing)
            {
                var durationSeconds = session.GetTimerDefaultForPhase() * 60;
                timerSnapshot = timerService.Start(retroSessionId, durationSeconds);
                await Clients.Group($"retro:{retroSessionId}").SendAsync("TimerUpdated", new
                {
                    status = timerSnapshot.Status.ToString().ToLowerInvariant(),
                    remainingSeconds = timerSnapshot.RemainingSeconds,
                    endTimeUtc = timerSnapshot.EndTimeUtc,
                });
            }
        }

        // Send timer snapshot to joining client
        await Clients.Caller.SendAsync("TimerSnapshot", new
        {
            status = timerSnapshot.Status.ToString().ToLowerInvariant(),
            remainingSeconds = timerSnapshot.RemainingSeconds,
            endTimeUtc = timerSnapshot.EndTimeUtc,
        });
    }

    public Task LeaveRetro(int retroSessionId)
    {
        var connections = ConnectionState.GetValueOrDefault(retroSessionId);
        connections?.TryRemove(Context.ConnectionId, out _);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"retro:{retroSessionId}");
    }

    public async Task ToggleReady(int retroSessionId)
    {
        var claimRetroSessionId = Context.User?.FindFirstValue("retroSessionId");
        if (!int.TryParse(claimRetroSessionId, out var parsedRetroSessionId) || parsedRetroSessionId != retroSessionId)
            throw new HubException("Not authorized for this retro.");

        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var displayNameClaim = Context.User?.FindFirstValue("displayName");
        if (!int.TryParse(participantIdClaim, out var participantId) || displayNameClaim is null)
            throw new HubException("Missing participant claims.");

        var readyDict = ReadinessState.GetOrAdd(retroSessionId, _ => new ConcurrentDictionary<int, string>());
        bool isReady;

        if (readyDict.ContainsKey(participantId))
        {
            readyDict.TryRemove(participantId, out _);
            isReady = false;
        }
        else
        {
            readyDict[participantId] = displayNameClaim;
            isReady = true;
        }

        var connections = ConnectionState.GetOrAdd(retroSessionId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections.Values.Distinct().Count();

        await Clients.Group($"retro:{retroSessionId}").SendAsync("ReadinessChanged", new
        {
            participantId,
            displayName = displayNameClaim,
            isReady,
            readyCount = readyDict.Count,
            totalCount,
        });
    }

    public async Task StartTimer(int retroSessionId)
    {
        await RequireScrumMasterAsync(retroSessionId);
        var session = await LoadTimedRetroAsync(retroSessionId);
        var snapshot = timerService.Start(retroSessionId, session.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(retroSessionId, snapshot);
    }

    public async Task PauseTimer(int retroSessionId)
    {
        await RequireScrumMasterAsync(retroSessionId);
        await LoadTimedRetroAsync(retroSessionId);
        var snapshot = timerService.Pause(retroSessionId);
        await BroadcastTimerUpdated(retroSessionId, snapshot);
    }

    public async Task ResumeTimer(int retroSessionId)
    {
        await RequireScrumMasterAsync(retroSessionId);
        await LoadTimedRetroAsync(retroSessionId);
        var snapshot = timerService.Resume(retroSessionId);
        await BroadcastTimerUpdated(retroSessionId, snapshot);
    }

    public async Task ResetTimer(int retroSessionId)
    {
        await RequireScrumMasterAsync(retroSessionId);
        var session = await LoadTimedRetroAsync(retroSessionId);
        var snapshot = timerService.Reset(retroSessionId, session.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(retroSessionId, snapshot);
    }

    public async Task AddTime(int retroSessionId, int minutes)
    {
        if (minutes is not (1 or 2 or 5))
            throw new HubException("Invalid minutes value. Allowed: 1, 2, 5.");
        await RequireScrumMasterAsync(retroSessionId);
        await LoadTimedRetroAsync(retroSessionId);
        var snapshot = timerService.AddTime(retroSessionId, minutes);
        await BroadcastTimerUpdated(retroSessionId, snapshot);
    }

    private async Task RequireScrumMasterAsync(int retroSessionId)
    {
        var claimRetroSessionId = Context.User?.FindFirstValue("retroSessionId");
        if (!int.TryParse(claimRetroSessionId, out var parsedRetroSessionId) || parsedRetroSessionId != retroSessionId)
            throw new HubException("Not authorized for this retro.");

        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        if (!int.TryParse(participantIdClaim, out var participantId))
            throw new HubException("Missing participant claims.");

        using var scope = scopeFactory.CreateScope();
        var participantRepo = scope.ServiceProvider.GetRequiredService<ParticipantRepository>();
        var retroSessionRepo = scope.ServiceProvider.GetRequiredService<RetroSessionRepository>();
        var teamRepo = scope.ServiceProvider.GetRequiredService<TeamRepository>();

        var participant = await participantRepo.GetByIdAsync(participantId, Context.ConnectionAborted);

        // Resolve the actor as an Identity Person id (Team.OwnerId is a Person id after RA2).
        // Participant.PersonId is set for authenticated joiners; otherwise fall back to the
        // cookie's person_id claim so a suite member without a participant row still resolves.
        int? resolvedPersonId = participant?.PersonId;
        if (resolvedPersonId is null)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext is not null)
            {
                var cookieResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (cookieResult.Succeeded)
                {
                    var personIdClaim = cookieResult.Principal?.FindFirstValue("person_id");
                    if (int.TryParse(personIdClaim, out var cookiePersonId))
                        resolvedPersonId = cookiePersonId;
                }
            }
        }

        if (resolvedPersonId is null)
            throw new HubException("Only the Scrum Master can control the timer.");

        var session = await retroSessionRepo.GetByIdAsync(retroSessionId, Context.ConnectionAborted);
        if (session is null)
            throw new HubException("Retro not found.");

        var team = await teamRepo.GetByIdAsync(session.TeamId, Context.ConnectionAborted);
        if (team is null || team.OwnerId != resolvedPersonId.Value)
            throw new HubException("Only the Scrum Master can control the timer.");
    }

    private async Task<Reflekt.Domain.RetroSessions.RetroSession> LoadTimedRetroAsync(int retroSessionId)
    {
        using var scope = scopeFactory.CreateScope();
        var retroSessionRepo = scope.ServiceProvider.GetRequiredService<RetroSessionRepository>();
        var session = await retroSessionRepo.GetByIdAsync(retroSessionId, Context.ConnectionAborted)
            ?? throw new HubException("Retro not found.");

        if (session.CurrentPhase is not (
            Reflekt.Domain.RetroSessions.RetroPhase.Writing or
            Reflekt.Domain.RetroSessions.RetroPhase.Voting or
            Reflekt.Domain.RetroSessions.RetroPhase.Discussing))
            throw new HubException($"Timer is not available in phase {session.CurrentPhase}.");

        return session;
    }

    private Task BroadcastTimerUpdated(int retroSessionId, TimerSnapshot snapshot) =>
        Clients.Group($"retro:{retroSessionId}").SendAsync("TimerUpdated", new
        {
            status = snapshot.Status.ToString().ToLowerInvariant(),
            remainingSeconds = snapshot.RemainingSeconds,
            endTimeUtc = snapshot.EndTimeUtc,
        });

    public static void ClearReadiness(int retroSessionId)
    {
        if (ReadinessState.TryGetValue(retroSessionId, out var readyDict))
            readyDict.Clear();
    }

    public static int GetParticipantCount(int retroSessionId)
    {
        if (ConnectionState.TryGetValue(retroSessionId, out var connections))
            return connections.Values.Distinct().Count();
        return 0;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var retroSessionIdClaim = Context.User?.FindFirstValue("retroSessionId");

        if (int.TryParse(participantIdClaim, out var participantId) &&
            int.TryParse(retroSessionIdClaim, out var retroSessionId))
        {
            // Remove from connection tracking
            var connections = ConnectionState.GetValueOrDefault(retroSessionId);
            connections?.TryRemove(Context.ConnectionId, out _);

            // Remove from readiness if present and no other connections remain for this participant
            var stillConnected = connections?.Values.Contains(participantId) ?? false;
            if (!stillConnected)
            {
                var readyDict = ReadinessState.GetValueOrDefault(retroSessionId);
                if (readyDict is not null && readyDict.TryRemove(participantId, out _))
                {
                    var totalCount = connections?.Values.Distinct().Count() ?? 0;
                    await Clients.Group($"retro:{retroSessionId}").SendAsync("ReadinessChanged", new
                    {
                        participantId,
                        displayName = (string?)null,
                        isReady = false,
                        readyCount = readyDict.Count,
                        totalCount,
                    });
                }
            }

            await Clients.Group($"retro:{retroSessionId}")
                .SendAsync("ParticipantLeft", new { participantId });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
