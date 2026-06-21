using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Participants;

namespace Poker.Domain.Planning;

/// <summary>
/// The planning ceremony. Aggregate root owning SessionParticipant and EstimationItem children.
/// Mode is set at creation and immutable (Sprint-bound | Free).
/// </summary>
public sealed partial class PlanningSession : AggregateRoot<Guid>
{
    private PlanningSession() { }

    public SessionMode Mode { get; private set; }

    /// <summary>Fokus sprint id; present only in Sprint-bound mode.</summary>
    public int? SprintId { get; private set; }

    /// <summary>Snapshot of the sprint name at session creation (Sprint-bound only).</summary>
    public string? SprintName { get; private set; }

    /// <summary>Human label. Required in Free mode; optional in Sprint-bound.</summary>
    public string? Title { get; private set; }

    /// <summary>Identity Person id of the SM who created the session. Never a guest (Rule 1).</summary>
    public required int CreatedByPersonId { get; init; }

    public SessionStatus Status { get; private set; } = SessionStatus.Active;

    public required DateTime CreatedAt { get; init; }

    public DateTime? ClosedAt { get; private set; }

    private readonly List<SessionParticipant> _participants = [];
    public IReadOnlyList<SessionParticipant> Participants => _participants.AsReadOnly();

    private readonly List<EstimationItem> _items = [];
    public IReadOnlyList<EstimationItem> Items => _items.AsReadOnly();
}
