using Poker.Domain.Planning.Events;
using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Participants;
using Poker.Domain.Planning.Votes;

namespace Poker.Domain.Planning;

/// <summary>Behavior methods for PlanningSession.</summary>
public sealed partial class PlanningSession
{
    /// <summary>
    /// Factory: Sprint-bound session. Requires a sprint id (Rule 1 / spec).
    /// </summary>
    public static PlanningSession CreateSprintBound(
        int createdByPersonId,
        int sprintId,
        string sprintName,
        string? title,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sprintName);

        var session = new PlanningSession
        {
            Id = Guid.NewGuid(),
            Mode = SessionMode.SprintBound,
            SprintId = sprintId,
            SprintName = sprintName,
            Title = title,
            CreatedByPersonId = createdByPersonId,
            Status = SessionStatus.Active,
            CreatedAt = createdAt,
        };
        return session;
    }

    /// <summary>
    /// Factory: Free-mode session. Title is required (spec API Surface / Rule 11).
    /// </summary>
    public static PlanningSession CreateFree(
        int createdByPersonId,
        string title,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var session = new PlanningSession
        {
            Id = Guid.NewGuid(),
            Mode = SessionMode.Free,
            SprintId = null,
            SprintName = null,
            Title = title,
            CreatedByPersonId = createdByPersonId,
            Status = SessionStatus.Active,
            CreatedAt = createdAt,
        };
        return session;
    }

    /// <summary>
    /// Add a participant who joined as an authenticated suite member (PersonId set).
    /// Idempotent: returns the existing participant if already joined (Rule 5 endpoint).
    /// </summary>
    public SessionParticipant AddAuthenticatedParticipant(int personId, string displayName, DateTime joinedAt)
    {
        EnsureActive();

        var existing = _participants.FirstOrDefault(p => p.PersonId == personId);
        if (existing is not null) return existing;

        var participant = SessionParticipant.JoinAuthenticated(Id, personId, displayName, joinedAt);
        _participants.Add(participant);
        return participant;
    }

    /// <summary>
    /// Add a guest participant (PersonId null, ADR-022). Display name must be unique per session.
    /// </summary>
    public SessionParticipant AddGuestParticipant(string displayName, DateTime joinedAt)
    {
        EnsureActive();

        var participant = SessionParticipant.JoinAsGuest(Id, displayName, joinedAt);
        _participants.Add(participant);
        return participant;
    }

    /// <summary>
    /// Add a Sprint-bound item (ticket from Jira). Idempotent by TicketKey (Rule 19).
    /// Re-adding a previously-removed ticket reuses its existing item.
    /// </summary>
    public EstimationItem AddSprintBoundItem(string ticketKey, string label, int order)
    {
        EnsureActive();
        EnsureSprintBound();

        var existing = _items.FirstOrDefault(i => i.TicketKey == ticketKey);
        if (existing is not null) return existing;

        var item = EstimationItem.Create(Id, label, order, ticketKey);
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Add a free-mode item (manually entered by SM). Guests cannot add items (Rule 3).
    /// </summary>
    public EstimationItem AddFreeItem(string label, int order)
    {
        EnsureActive();
        if (Mode != SessionMode.Free)
            throw new InvalidOperationException("Manual items can only be added to Free-mode sessions.");

        var item = EstimationItem.Create(Id, label, order);
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// SM selects an item to estimate. Raises ItemSelectedEvent for SignalR broadcast.
    /// </summary>
    public EstimationItem SelectItem(Guid itemId)
    {
        EnsureActive();
        var item = RequireItem(itemId);
        AddDomainEvent(new ItemSelectedEvent(Id, item.Id, item.Label));
        return item;
    }

    /// <summary>
    /// Reveal votes on the active item. Transitions item Voting → Revealed.
    /// Raises VotesRevealedEvent (carries the full vote tally for broadcast).
    /// </summary>
    public void RevealVotes(Guid itemId, IReadOnlyList<Participants.SessionParticipant> participants)
    {
        EnsureActive();
        var item = RequireItem(itemId);
        item.Reveal();

        var results = item.Votes
            .Join(participants,
                v => v.ParticipantId,
                p => p.Id,
                (v, p) => new VoteResult(p.Id, p.DisplayName, v.Value))
            .ToList();

        AddDomainEvent(new VotesRevealedEvent(Id, itemId, results));
    }

    /// <summary>
    /// Decide the final estimate. Transitions item Revealed → Decided and raises EstimateDecidedEvent.
    /// Sprint-bound: caller must have confirmed Jira write-back before calling this (Rule 10).
    /// </summary>
    public void DecideEstimate(Guid itemId, string value, DateTime decidedAt)
    {
        EnsureActive();
        var item = RequireItem(itemId);
        item.Decide(value, decidedAt);
        AddDomainEvent(new EstimateDecidedEvent(Id, itemId, value));
    }

    /// <summary>
    /// Skip the current item (SM only, Rule 9). Raises ItemSkippedEvent.
    /// </summary>
    public void SkipItem(Guid itemId)
    {
        EnsureActive();
        var item = RequireItem(itemId);
        item.Skip();
        AddDomainEvent(new ItemSkippedEvent(Id, itemId));
    }

    /// <summary>
    /// Close the session (SM only, Rule 14). Transitions Active → Closed.
    /// Raises SessionClosedEvent with summary stats.
    /// </summary>
    public void Close(DateTime closedAt)
    {
        if (Status == SessionStatus.Closed)
            throw new InvalidOperationException("Session is already closed.");

        Status = SessionStatus.Closed;
        ClosedAt = closedAt;

        var estimated = _items.Count(i => i.Status == ItemStatus.Decided);
        var skipped = _items.Count(i => i.Status == ItemStatus.Skipped);
        var totalPoints = _items
            .Where(i => i.Status == ItemStatus.Decided && i.FinalEstimate is not null)
            .Sum(i => decimal.TryParse(i.FinalEstimate, out var v) ? v : 0m);

        AddDomainEvent(new SessionClosedEvent(Id, estimated, skipped, totalPoints));
    }

    // ---- Guards ----

    private void EnsureActive()
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException("The session is closed and no longer accepts changes.");
    }

    private void EnsureSprintBound()
    {
        if (Mode != SessionMode.SprintBound)
            throw new InvalidOperationException("This operation is only available in Sprint-bound mode.");
    }

    private EstimationItem RequireItem(Guid itemId)
        => _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new ArgumentException($"Item {itemId} not found in this session.", nameof(itemId));
}
