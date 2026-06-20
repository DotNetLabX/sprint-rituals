
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

    public RetroColumn Column1 { get; private set; } = RetroColumn.Create("Went Well", "green");
    public RetroColumn Column2 { get; private set; } = RetroColumn.Create("Didn't Go Well", "red");
    public RetroColumn Column3 { get; private set; } = RetroColumn.Create("Start Doing", "amber");

    public TimerDefaults Timers { get; private set; } = TimerDefaults.Create(5, 3, 10);

    private readonly List<FokusSprintRef> _fokusSprintRefs = [];
    public IReadOnlyList<FokusSprintRef> FokusSprintRefs => _fokusSprintRefs.AsReadOnly();
}
