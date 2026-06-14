
namespace Reflekt.Domain.Participants;

public sealed partial class Participant : AggregateRoot
{
    private Participant() { }

    public required int SprintId { get; init; }
    public required string DisplayName { get; init; }
    public required string GuestToken { get; init; }
    public required DateTime JoinedAt { get; init; }

    /// <summary>Identity <c>Person</c> id for an authenticated participant; null for guests (ADR-022). RA2/ADR-016.</summary>
    public int? PersonId { get; init; }
}
