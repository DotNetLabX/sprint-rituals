
namespace Reflekt.Domain.RetroSessions;

public sealed partial class RetroSession : AggregateRoot
{
    private RetroSession() { }

    public required int TeamId { get; init; }
    public required string Name { get; init; }
    public RetroSessionState State { get; private set; }
    public RetroPhase CurrentPhase { get; private set; }
    public required int VotesPerParticipant { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; private set; }
    public int? FocusedGroupId { get; private set; }

    public string Column1Name { get; private set; } = "Went Well";
    public string Column2Name { get; private set; } = "Didn't Go Well";
    public string Column3Name { get; private set; } = "Start Doing";
    public ColumnColor Column1Color { get; private set; } = ColumnColor.Create("green");
    public ColumnColor Column2Color { get; private set; } = ColumnColor.Create("red");
    public ColumnColor Column3Color { get; private set; } = ColumnColor.Create("amber");

    public required int WritingTimerDefault { get; init; }
    public required int VotingTimerDefault { get; init; }
    public required int DiscussingTimerDefault { get; init; }

    private readonly List<FokusSprintRef> _fokusSprintRefs = [];
    public IReadOnlyList<FokusSprintRef> FokusSprintRefs => _fokusSprintRefs.AsReadOnly();
}
