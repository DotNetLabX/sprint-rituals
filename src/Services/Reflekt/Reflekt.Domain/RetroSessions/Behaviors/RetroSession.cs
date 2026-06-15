namespace Reflekt.Domain.RetroSessions;

public sealed partial class RetroSession
{
    private const int WritingTimerMin = 1;
    private const int WritingTimerMax = 60;
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

        if (writingTimerDefault.HasValue)
            ValidateTimerRange(writingTimerDefault.Value, nameof(writingTimerDefault));
        if (votingTimerDefault.HasValue)
            ValidateTimerRange(votingTimerDefault.Value, nameof(votingTimerDefault));
        if (discussingTimerDefault.HasValue)
            ValidateTimerRange(discussingTimerDefault.Value, nameof(discussingTimerDefault));

        var session = new RetroSession
        {
            Name = name,
            TeamId = teamId,
            VotesPerParticipant = votesPerParticipant,
            CreatedAt = DateTime.UtcNow,
            WritingTimerDefault = writingTimerDefault ?? DefaultWritingTimer,
            VotingTimerDefault = votingTimerDefault ?? DefaultVotingTimer,
            DiscussingTimerDefault = discussingTimerDefault ?? DefaultDiscussingTimer,
        };
        session.State = RetroSessionState.Draft;
        session.CurrentPhase = RetroPhase.Writing;

        if (column1Name is not null) session.Column1Name = ValidateColumnName(column1Name, nameof(column1Name));
        if (column1Color is not null) session.Column1Color = ColumnColor.Create(column1Color);
        if (column2Name is not null) session.Column2Name = ValidateColumnName(column2Name, nameof(column2Name));
        if (column2Color is not null) session.Column2Color = ColumnColor.Create(column2Color);
        if (column3Name is not null) session.Column3Name = ValidateColumnName(column3Name, nameof(column3Name));
        if (column3Color is not null) session.Column3Color = ColumnColor.Create(column3Color);

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

    public int GetTimerDefaultForPhase() => CurrentPhase switch
    {
        RetroPhase.Writing => WritingTimerDefault,
        RetroPhase.Voting => VotingTimerDefault,
        RetroPhase.Discussing => DiscussingTimerDefault,
        _ => throw new InvalidOperationException($"Phase {CurrentPhase} does not have a timer default."),
    };

    private static void ValidateTimerRange(int value, string paramName)
    {
        if (value < WritingTimerMin || value > WritingTimerMax)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Timer default must be between {WritingTimerMin} and {WritingTimerMax} minutes.");
    }

    public void UpdateColumnConfig(
        string column1Name, string column1Color,
        string column2Name, string column2Color,
        string column3Name, string column3Color)
    {
        Column1Name = ValidateColumnName(column1Name, nameof(column1Name));
        Column1Color = ColumnColor.Create(column1Color);
        Column2Name = ValidateColumnName(column2Name, nameof(column2Name));
        Column2Color = ColumnColor.Create(column2Color);
        Column3Name = ValidateColumnName(column3Name, nameof(column3Name));
        Column3Color = ColumnColor.Create(column3Color);
        AddDomainEvent(new Events.ColumnConfigUpdatedEvent(this));
    }

    private static string ValidateColumnName(string name, string paramName)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Column name cannot be empty or whitespace.", paramName);
        if (trimmed.Length > 30)
            throw new ArgumentException("Column name cannot exceed 30 characters.", paramName);
        return trimmed;
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
