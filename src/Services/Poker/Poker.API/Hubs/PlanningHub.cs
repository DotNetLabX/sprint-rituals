using System.Collections.Concurrent;
using System.Security.Claims;
using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Poker.API.Auth;
using Poker.Domain.Planning.Items;
using Poker.Persistence.Repositories;

namespace Poker.API.Hubs;

/// <summary>
/// Real-time estimation loop for a planning session.
/// Route: /hubs/poker. Group key: poker:{sessionId}.
/// Accepts cookie auth (SM, authenticated participants) or Poker guest token.
/// Connection state is in-process (ConcurrentDictionary) — matches RetroHub pattern.
/// </summary>
[Authorize(AuthenticationSchemes =
    $"{CookieAuthenticationDefaults.AuthenticationScheme},{PokerGuestTokenDefaults.AuthenticationScheme}")]
public class PlanningHub(
    ILogger<PlanningHub> logger,
    IServiceScopeFactory scopeFactory) : Hub
{
    // sessionId → connectionId → participantId (Guid)
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, Guid>> ConnectionState = new();

    // ---- Connection lifecycle ----

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to PlanningHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task JoinSession(Guid sessionId)
    {
        // Validate the claim matches the requested session (guest token carries sessionId claim)
        var claimSessionId = GetClaimSessionId();
        if (claimSessionId is not null && claimSessionId != sessionId)
            throw new HubException("Not authorized to join this session.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupKey(sessionId));

        // Resolve participantId — guest token carries it as a claim; cookie-authed users are
        // looked up by PersonId so they are tracked in ConnectionState and counted for auto-reveal.
        var participantId = await ResolveParticipantIdAsync(sessionId);
        if (participantId.HasValue)
        {
            var connections = ConnectionState.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, Guid>());
            connections[Context.ConnectionId] = participantId.Value;
        }

        // Broadcast participant joined — other clients update their participant list
        if (participantId.HasValue)
        {
            var displayName = Context.User?.FindFirstValue("displayName")
                ?? Context.User?.FindFirstValue(ClaimTypes.Name)
                ?? "Unknown";

            await Clients.Group(GroupKey(sessionId))
                .SendAsync("ParticipantJoined", new { participantId = participantId.Value, displayName });
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var claimSessionId = GetClaimSessionId();

        if (claimSessionId.HasValue)
        {
            var sessionId = claimSessionId.Value;
            // Resolve participantId same way JoinSession does — covers guests AND cookie-authed users
            var participantId = await ResolveParticipantIdAsync(sessionId);

            var connections = ConnectionState.GetValueOrDefault(sessionId);
            connections?.TryRemove(Context.ConnectionId, out _);

            // Rule 13: session stays Active; notify others (only if we resolved the participant)
            if (participantId.HasValue)
            {
                await Clients.Group(GroupKey(sessionId))
                    .SendAsync("ParticipantLeft", new { participantId = participantId.Value });
            }

            // Auto-reveal check on disconnect (Rule 7): if the disconnecting voter was the last one
            // needed for auto-reveal, all remaining connected voters may already have voted.
            await TryAutoRevealOnDisconnectAsync(sessionId, connections);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ---- Client → Server methods ----

    public async Task SubmitVote(Guid sessionId, Guid itemId, string value)
    {
        var participantId = await RequireParticipantIdAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var itemRepo = scope.ServiceProvider.GetRequiredService<EstimationItemRepository>();

        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        var item = session.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new HubException("Item not found.");

        // CastVote upserts — Rule 21 enforced in domain
        item.CastVote(participantId, value, DateTime.UtcNow);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);

        // Vote privacy (Rule 7): broadcast participant id only, no value until reveal
        await Clients.Group(GroupKey(sessionId))
            .SendAsync("VoteReceived", new { participantId, itemId });

        // Auto-reveal (Rule 7): if every connected voter has voted, reveal automatically
        var connections = ConnectionState.GetValueOrDefault(sessionId);
        var connectedVoterIds = GetConnectedVoterIds(session, connections);
        if (connectedVoterIds.Count > 0 && item.AllPresentVotersHaveVoted(connectedVoterIds))
            await RevealItemAsync(sessionId, itemId, sessionRepo, session);
    }

    /// <summary>
    /// Replace a previously cast vote with a new value (Rule 21 — one vote per participant per item).
    /// Only valid pre-reveal; the domain's CastVote upsert handles the replacement.
    /// Post-reveal changes are rejected by EstimationItem.CastVote (domain guard).
    /// </summary>
    public async Task ChangeVote(Guid sessionId, Guid itemId, string newValue)
        => await SubmitVote(sessionId, itemId, newValue);

    public async Task SelectItem(Guid sessionId, Guid itemId)
    {
        await RequireScrumMasterAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        session.SelectItem(itemId);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);
        // ItemSelectedEvent handler (Step 8) broadcasts ItemSelected to the group
    }

    public async Task RevealVotes(Guid sessionId, Guid itemId)
    {
        await RequireScrumMasterAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        await RevealItemAsync(sessionId, itemId, sessionRepo, session);
    }

    public async Task DecideEstimate(Guid sessionId, Guid itemId, string value)
    {
        await RequireScrumMasterAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        var item = session.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new HubException("Item not found.");

        // Sprint-bound: write-back to Jira must succeed BEFORE marking Decided (Rule 10)
        if (session.Mode == SessionMode.SprintBound && item.TicketKey is not null)
        {
            var writeBackOk = await TrySetStoryPointsAsync(item.TicketKey, value, scope);
            if (!writeBackOk)
            {
                // Item remains not-Decided; SM may retry (Rule 10)
                await Clients.Group(GroupKey(sessionId))
                    .SendAsync("WriteBackFailed", new { itemId, reason = "Jira write-back failed." });
                return;
            }
        }

        // Only reach here after confirmed write-back (Sprint-bound) or Free mode (no write-back)
        session.DecideEstimate(itemId, value, DateTime.UtcNow);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);
        // EstimateDecidedEvent handler (Step 8) broadcasts EstimateDecided
    }

    public async Task SkipItem(Guid sessionId, Guid itemId)
    {
        await RequireScrumMasterAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        session.SkipItem(itemId);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);
        // ItemSkippedEvent handler (Step 8) broadcasts ItemSkipped
    }

    public async Task CloseSession(Guid sessionId)
    {
        await RequireScrumMasterAsync(sessionId);

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        if (session.Status == SessionStatus.Closed)
            throw new HubException("Session is already closed.");

        session.Close(DateTime.UtcNow);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);
        // SessionClosedEvent handler (Step 8) broadcasts SessionClosed with summary
    }

    // ---- Private helpers ----

    private static string GroupKey(Guid sessionId) => $"poker:{sessionId}";

    /// <summary>
    /// Returns the participant Guid from the guest-token claim, or null if the caller is cookie-authed.
    /// </summary>
    private Guid? GetParticipantIdFromGuestClaim()
    {
        var claim = Context.User?.FindFirstValue("participantId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? GetClaimSessionId()
    {
        var claim = Context.User?.FindFirstValue("sessionId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Resolves the participant Guid for the current caller.
    /// Guest-token callers: read from the "participantId" claim (set by PokerGuestTokenAuthenticationHandler).
    /// Cookie-authed callers: look up SessionParticipant by PersonId — they joined via REST first.
    /// Returns null if the participant cannot be resolved (e.g. not yet joined).
    /// </summary>
    private async Task<Guid?> ResolveParticipantIdAsync(Guid sessionId)
    {
        // Fast path — guest token carries the participantId as a claim
        var fromClaim = GetParticipantIdFromGuestClaim();
        if (fromClaim.HasValue) return fromClaim;

        // Cookie-authed path — look up by PersonId
        var personIdStr = Context.User?.FindFirstValue("person_id");
        if (!int.TryParse(personIdStr, out var personId)) return null;

        using var scope = scopeFactory.CreateScope();
        var participantRepo = scope.ServiceProvider.GetRequiredService<SessionParticipantRepository>();
        var participant = await participantRepo.GetByPersonIdInSessionAsync(sessionId, personId, Context.ConnectionAborted);
        return participant?.Id;
    }

    /// <summary>
    /// Requires the participant Guid — throws HubException if it cannot be resolved.
    /// </summary>
    private async Task<Guid> RequireParticipantIdAsync(Guid sessionId)
    {
        return await ResolveParticipantIdAsync(sessionId)
            ?? throw new HubException("Participant identity could not be resolved.");
    }

    private async Task RequireScrumMasterAsync(Guid sessionId)
    {
        // SM gate: resolve caller's Identity Person id from cookie (mirror RetroHub.RequireScrumMasterAsync)
        int? personId = null;

        var httpContext = Context.GetHttpContext();
        if (httpContext is not null)
        {
            var cookieResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (cookieResult.Succeeded)
            {
                var personIdClaim = cookieResult.Principal?.FindFirstValue("person_id");
                if (int.TryParse(personIdClaim, out var pid))
                    personId = pid;
            }
        }

        if (personId is null)
            throw new HubException("Only the Scrum Master can perform this action.");

        using var scope = scopeFactory.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
        var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted)
            ?? throw new HubException("Session not found.");

        if (session.CreatedByPersonId != personId.Value)
            throw new HubException("Only the Scrum Master can perform this action.");
    }

    /// <summary>
    /// Reveal votes on an item and save. Domain raises VotesRevealedEvent; Step-8 handler broadcasts.
    /// </summary>
    private async Task RevealItemAsync(
        Guid sessionId,
        Guid itemId,
        PlanningSessionRepository sessionRepo,
        PlanningSession session)
    {
        var item = session.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new HubException("Item not found.");

        if (item.Status != ItemStatus.Voting)
            return; // Already revealed — idempotent

        session.RevealVotes(itemId, session.Participants);
        await sessionRepo.SaveChangesAsync(Context.ConnectionAborted);
        // VotesRevealedEvent handler (Step 8) broadcasts VotesRevealed
    }

    /// <summary>
    /// Auto-reveal check on disconnect: if all remaining connected voters have already voted, auto-reveal.
    /// Rule 7 — disconnect case.
    /// </summary>
    private async Task TryAutoRevealOnDisconnectAsync(
        Guid sessionId,
        ConcurrentDictionary<string, Guid>? connections)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sessionRepo = scope.ServiceProvider.GetRequiredService<PlanningSessionRepository>();
            var session = await sessionRepo.GetByIdWithDetailsAsync(sessionId, Context.ConnectionAborted);
            if (session is null || session.Status == SessionStatus.Closed) return;

            // Find the currently-active item (Voting status)
            var activeItem = session.Items.FirstOrDefault(i => i.Status == ItemStatus.Voting);
            if (activeItem is null) return;

            var connectedVoterIds = GetConnectedVoterIds(session, connections);
            if (connectedVoterIds.Count > 0 && activeItem.AllPresentVotersHaveVoted(connectedVoterIds))
                await RevealItemAsync(sessionId, activeItem.Id, sessionRepo, session);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Auto-reveal on disconnect check failed for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Attempt to write story points to Jira via Fokus gRPC.
    /// Returns true on success, false on any Jira-side failure.
    /// </summary>
    private static async Task<bool> TrySetStoryPointsAsync(string ticketKey, string value, IServiceScope scope)
    {
        if (!decimal.TryParse(value, out var points))
            return false; // "?" or non-numeric — guard (should never reach here after domain validation)

        var fokusService = scope.ServiceProvider.GetRequiredService<IPokerFokusService>();
        var response = await fokusService.SetStoryPointsAsync(
            new SetStoryPointsRequest { IssueKey = ticketKey, StoryPoints = points });

        return response.Success;
    }

    /// <summary>
    /// Returns the set of participant Guids currently connected and flagged as voters.
    /// Uses the session's participant list to filter voters (IsVoter = always true in v1).
    /// </summary>
    private static IReadOnlySet<Guid> GetConnectedVoterIds(
        PlanningSession session,
        ConcurrentDictionary<string, Guid>? connections)
    {
        if (connections is null || connections.IsEmpty) return new HashSet<Guid>();

        var connectedIds = connections.Values.ToHashSet();
        // Filter to session participants who are voters (IsVoter = true for everyone in v1)
        var voterIds = session.Participants
            .Where(p => p.IsVoter && connectedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSet();

        return voterIds;
    }

    /// <summary>
    /// Cleans up connection state when a session is closed (called from close flow if needed).
    /// </summary>
    public static void RemoveSession(Guid sessionId) => ConnectionState.TryRemove(sessionId, out _);
}
