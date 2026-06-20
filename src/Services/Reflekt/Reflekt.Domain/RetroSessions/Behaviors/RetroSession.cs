namespace Reflekt.Domain.RetroSessions;

public sealed partial class RetroSession
{
    private const int DefaultWritingTimer = 5;
    private const int DefaultVotingTimer = 3;
    private const int DefaultDiscussingTimer = 10;

    public static RetroSession Create(
        string name,
        int teamId,
        IReadOnlyCollection<int> fokusSprintIds,
        int votesPerParticipant = 3,
        string? column1Name = null,
        string? column1Color = null,
        string? column2Name = null,
        string? column2Color = null,
        string? column3Name = null,
        string? column3Color = null,
        int? writingTimerDefault = null,
        int? votingTimerDefault = null,
        int? discussingTimerDefault = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(votesPerParticipant, 1);

        if (fokusSprintIds is null || fokusSprintIds.Count == 0)
            throw new ArgumentException("A RetroSession must reference at least one Fokus sprint.", nameof(fokusSprintIds));

        var session = new RetroSession
        {
            Name = name,
            TeamId = teamId,
            VotesPerParticipant = votesPerParticipant,
            CreatedAt = DateTime.UtcNow,
        };
        session.State = RetroSessionState.Draft;
        session.CurrentPhase = RetroPhase.Writing;

        // Group columns into VOs; explicit values override defaults (validation inside RetroColumn.Create).
        session.Column1 = RetroColumn.Create(column1Name ?? "Went Well", column1Color ?? "green");
        session.Column2 = RetroColumn.Create(column2Name ?? "Didn't Go Well", column2Color ?? "red");
        session.Column3 = RetroColumn.Create(column3Name ?? "Start Doing", column3Color ?? "amber");

        // Group timer defaults into VO (1–60 validation inside TimerDefaults.Create).
        session.Timers = TimerDefaults.Create(
            writingTimerDefault ?? DefaultWritingTimer,
            votingTimerDefault ?? DefaultVotingTimer,
            discussingTimerDefault ?? DefaultDiscussingTimer);

        foreach (var fokusSprintId in fokusSprintIds)
            session._fokusSprintRefs.Add(new FokusSprintRef(fokusSprintId));

        session.AddDomainEvent(new Events.RetroSessionCreatedEvent(session));
        return session;
    }

    /// <summary>Adds a Fokus sprint reference to an existing session (for the "skipped-retro covers two sprints" case).</summary>
    public void AddFokusSprintRef(int fokusSprintId)
    {
        if (_fokusSprintRefs.Any(r => r.FokusSprintId == fokusSprintId))
            return;
        _fokusSprintRefs.Add(new FokusSprintRef(fokusSprintId));
    }

    /// <summary>Returns the timer default for the current phase. Delegates to <see cref="TimerDefaults.GetForPhase"/>.</summary>
    public int GetTimerDefaultForPhase() => Timers.GetForPhase(CurrentPhase);

    public void UpdateColumnConfig(
        string column1Name, string column1Color,
        string column2Name, string column2Color,
        string column3Name, string column3Color)
    {
        // Validation (non-empty, ≤30 chars, palette guard) enforced inside RetroColumn.Create.
        Column1 = RetroColumn.Create(column1Name, column1Color);
        Column2 = RetroColumn.Create(column2Name, column2Color);
        Column3 = RetroColumn.Create(column3Name, column3Color);
        AddDomainEvent(new Events.ColumnConfigUpdatedEvent(this));
    }

    public void Start()
    {
        if (State != RetroSessionState.Draft)
            throw new InvalidOperationException("RetroSession must be in Draft state to start.");
        State = RetroSessionState.Active;
        AddDomainEvent(new Events.RetroSessionStartedEvent(this));
    }

    public void AdvancePhase()
    {
        if (State != RetroSessionState.Active)
            throw new InvalidOperationException("RetroSession must be Active to advance phase.");
        CurrentPhase = CurrentPhase switch
        {
            RetroPhase.Writing => RetroPhase.Grouping,
            RetroPhase.Grouping => RetroPhase.Voting,
            RetroPhase.Voting => RetroPhase.Discussing,
            RetroPhase.Discussing => RetroPhase.Reporting,
            RetroPhase.Reporting => throw new InvalidOperationException("RetroSession phase is already Reporting."),
            _ => throw new InvalidOperationException($"Unknown phase: {CurrentPhase}."),
        };
        AddDomainEvent(new Events.PhaseAdvancedEvent(this));
    }

    public void Close()
    {
        if (State != RetroSessionState.Active)
            throw new InvalidOperationException("RetroSession must be Active to close.");
        State = RetroSessionState.Closed;
        ClosedAt = DateTime.UtcNow;
        AddDomainEvent(new Events.RetroSessionClosedEvent(this));
    }

    public void SetFocus(int groupId)
    {
        if (State != RetroSessionState.Active)
            throw new InvalidOperationException("RetroSession must be Active to set focus.");
        if (CurrentPhase != RetroPhase.Discussing)
            throw new InvalidOperationException("RetroSession must be in Discussing phase to set focus.");

        var previousGroupId = FocusedGroupId;
        FocusedGroupId = groupId;
        AddDomainEvent(new Events.GroupFocusedEvent(this, groupId, previousGroupId));
    }

    public void ClearFocus()
    {
        if (State != RetroSessionState.Active)
            throw new InvalidOperationException("RetroSession must be Active to clear focus.");
        if (CurrentPhase != RetroPhase.Discussing)
            throw new InvalidOperationException("RetroSession must be in Discussing phase to clear focus.");

        var previousGroupId = FocusedGroupId;
        FocusedGroupId = null;
        AddDomainEvent(new Events.GroupFocusClearedEvent(this, previousGroupId));
    }
}
