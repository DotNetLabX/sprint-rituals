using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.API.Features.Sprints.Timer;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Hubs;

[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]
public class SprintHub(
    ILogger<SprintHub> logger,
    TimerService timerService,
    IServiceScopeFactory scopeFactory) : Hub
{
    // sprintId -> participantId -> displayName (ready participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> ReadinessState = new();

    // sprintId -> connectionId -> participantId (all connected participants)
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> ConnectionState = new();

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to SprintHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task JoinSprint(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized to join this sprint.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

        // Track connection
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        if (int.TryParse(participantIdClaim, out var participantId))
        {
            var connections = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
            connections[Context.ConnectionId] = participantId;
        }

        // Send readiness snapshot to joining client
        var readyDict = ReadinessState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<int, string>());
        var connections2 = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections2.Values.Distinct().Count();

        await Clients.Caller.SendAsync("ReadinessSnapshot", new
        {
            readyParticipantIds = readyDict.Keys.ToList(),
            readyCount = readyDict.Count,
            totalCount,
        });

        // Auto-start timer if timed phase and timer not yet started
        var timerSnapshot = timerService.GetSnapshot(sprintId);
        if (timerSnapshot.Status == TimerStatus.Stopped)
        {
            using var scope = scopeFactory.CreateScope();
            var sprintRepo = scope.ServiceProvider.GetRequiredService<SprintRepository>();
            var sprint = await sprintRepo.GetByIdAsync(sprintId, Context.ConnectionAborted);
            if (sprint is not null && sprint.CurrentPhase is
                Reflekt.Domain.Sprints.SprintPhase.Writing or
                Reflekt.Domain.Sprints.SprintPhase.Voting or
                Reflekt.Domain.Sprints.SprintPhase.Discussing)
            {
                var durationSeconds = sprint.GetTimerDefaultForPhase() * 60;
                timerSnapshot = timerService.Start(sprintId, durationSeconds);
                await Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", new
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

    public Task LeaveSprint(int sprintId)
    {
        var connections = ConnectionState.GetValueOrDefault(sprintId);
        connections?.TryRemove(Context.ConnectionId, out _);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    public async Task ToggleReady(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized for this sprint.");

        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var displayNameClaim = Context.User?.FindFirstValue("displayName");
        if (!int.TryParse(participantIdClaim, out var participantId) || displayNameClaim is null)
            throw new HubException("Missing participant claims.");

        var readyDict = ReadinessState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<int, string>());
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

        var connections = ConnectionState.GetOrAdd(sprintId, _ => new ConcurrentDictionary<string, int>());
        var totalCount = connections.Values.Distinct().Count();

        await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
        {
            participantId,
            displayName = displayNameClaim,
            isReady,
            readyCount = readyDict.Count,
            totalCount,
        });
    }

    public async Task StartTimer(int sprintId)
    {
        await RequireScrumMasterAsync(sprintId);
        var sprint = await LoadTimedSprintAsync(sprintId);
        var snapshot = timerService.Start(sprintId, sprint.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task PauseTimer(int sprintId)
    {
        await RequireScrumMasterAsync(sprintId);
        await LoadTimedSprintAsync(sprintId);
        var snapshot = timerService.Pause(sprintId);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task ResumeTimer(int sprintId)
    {
        await RequireScrumMasterAsync(sprintId);
        await LoadTimedSprintAsync(sprintId);
        var snapshot = timerService.Resume(sprintId);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task ResetTimer(int sprintId)
    {
        await RequireScrumMasterAsync(sprintId);
        var sprint = await LoadTimedSprintAsync(sprintId);
        var snapshot = timerService.Reset(sprintId, sprint.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task AddTime(int sprintId, int minutes)
    {
        if (minutes is not (1 or 2 or 5))
            throw new HubException("Invalid minutes value. Allowed: 1, 2, 5.");
        await RequireScrumMasterAsync(sprintId);
        await LoadTimedSprintAsync(sprintId);
        var snapshot = timerService.AddTime(sprintId, minutes);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    private async Task RequireScrumMasterAsync(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized for this sprint.");

        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        if (!int.TryParse(participantIdClaim, out var participantId))
            throw new HubException("Missing participant claims.");

        using var scope = scopeFactory.CreateScope();
        var participantRepo = scope.ServiceProvider.GetRequiredService<ParticipantRepository>();
        var sprintRepo = scope.ServiceProvider.GetRequiredService<SprintRepository>();
        var teamRepo = scope.ServiceProvider.GetRequiredService<TeamRepository>();

        var participant = await participantRepo.GetByIdAsync(participantId, Context.ConnectionAborted);

        int? resolvedUserId = participant?.UserId;
        if (resolvedUserId is null)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext is not null)
            {
                var cookieResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (cookieResult.Succeeded)
                {
                    var userIdClaim = cookieResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out var cookieUserId))
                        resolvedUserId = cookieUserId;
                }
            }
        }

        if (resolvedUserId is null)
            throw new HubException("Only the Scrum Master can control the timer.");

        var sprint = await sprintRepo.GetByIdAsync(sprintId, Context.ConnectionAborted);
        if (sprint is null)
            throw new HubException("Sprint not found.");

        var team = await teamRepo.GetByIdAsync(sprint.TeamId, Context.ConnectionAborted);
        if (team is null || team.OwnerId != resolvedUserId.Value)
            throw new HubException("Only the Scrum Master can control the timer.");
    }

    private async Task<Reflekt.Domain.Sprints.Sprint> LoadTimedSprintAsync(int sprintId)
    {
        using var scope = scopeFactory.CreateScope();
        var sprintRepo = scope.ServiceProvider.GetRequiredService<SprintRepository>();
        var sprint = await sprintRepo.GetByIdAsync(sprintId, Context.ConnectionAborted)
            ?? throw new HubException("Sprint not found.");

        if (sprint.CurrentPhase is not (
            Reflekt.Domain.Sprints.SprintPhase.Writing or
            Reflekt.Domain.Sprints.SprintPhase.Voting or
            Reflekt.Domain.Sprints.SprintPhase.Discussing))
            throw new HubException($"Timer is not available in phase {sprint.CurrentPhase}.");

        return sprint;
    }

    private Task BroadcastTimerUpdated(int sprintId, TimerSnapshot snapshot) =>
        Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", new
        {
            status = snapshot.Status.ToString().ToLowerInvariant(),
            remainingSeconds = snapshot.RemainingSeconds,
            endTimeUtc = snapshot.EndTimeUtc,
        });

    public static void ClearReadiness(int sprintId)
    {
        if (ReadinessState.TryGetValue(sprintId, out var readyDict))
            readyDict.Clear();
    }

    public static int GetParticipantCount(int sprintId)
    {
        if (ConnectionState.TryGetValue(sprintId, out var connections))
            return connections.Values.Distinct().Count();
        return 0;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var sprintIdClaim = Context.User?.FindFirstValue("sprintId");

        if (int.TryParse(participantIdClaim, out var participantId) &&
            int.TryParse(sprintIdClaim, out var sprintId))
        {
            // Remove from connection tracking
            var connections = ConnectionState.GetValueOrDefault(sprintId);
            connections?.TryRemove(Context.ConnectionId, out _);

            // Remove from readiness if present and no other connections remain for this participant
            var stillConnected = connections?.Values.Contains(participantId) ?? false;
            if (!stillConnected)
            {
                var readyDict = ReadinessState.GetValueOrDefault(sprintId);
                if (readyDict is not null && readyDict.TryRemove(participantId, out _))
                {
                    var totalCount = connections?.Values.Distinct().Count() ?? 0;
                    await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
                    {
                        participantId,
                        displayName = (string?)null,
                        isReady = false,
                        readyCount = readyDict.Count,
                        totalCount,
                    });
                }
            }

            await Clients.Group($"sprint:{sprintId}")
                .SendAsync("ParticipantLeft", new { participantId });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
