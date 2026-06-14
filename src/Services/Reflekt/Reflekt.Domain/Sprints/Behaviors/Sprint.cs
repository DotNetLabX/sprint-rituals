namespace Reflekt.Domain.Sprints;

public sealed partial class Sprint
{
    private const int WritingTimerMin = 1;
    private const int WritingTimerMax = 60;
    private const int DefaultWritingTimer = 5;
    private const int DefaultVotingTimer = 3;
    private const int DefaultDiscussingTimer = 10;

    public static Sprint Create(
        string name,
        int teamId,
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

        if (writingTimerDefault.HasValue)
            ValidateTimerRange(writingTimerDefault.Value, nameof(writingTimerDefault));
        if (votingTimerDefault.HasValue)
            ValidateTimerRange(votingTimerDefault.Value, nameof(votingTimerDefault));
        if (discussingTimerDefault.HasValue)
            ValidateTimerRange(discussingTimerDefault.Value, nameof(discussingTimerDefault));

        var sprint = new Sprint
        {
            Name = name,
            TeamId = teamId,
            VotesPerParticipant = votesPerParticipant,
            CreatedAt = DateTime.UtcNow,
            WritingTimerDefault = writingTimerDefault ?? DefaultWritingTimer,
            VotingTimerDefault = votingTimerDefault ?? DefaultVotingTimer,
            DiscussingTimerDefault = discussingTimerDefault ?? DefaultDiscussingTimer,
        };
        sprint.State = SprintState.Draft;
        sprint.CurrentPhase = SprintPhase.Writing;

        if (column1Name is not null) sprint.Column1Name = ValidateColumnName(column1Name, nameof(column1Name));
        if (column1Color is not null) sprint.Column1Color = ColumnColor.Create(column1Color);
        if (column2Name is not null) sprint.Column2Name = ValidateColumnName(column2Name, nameof(column2Name));
        if (column2Color is not null) sprint.Column2Color = ColumnColor.Create(column2Color);
        if (column3Name is not null) sprint.Column3Name = ValidateColumnName(column3Name, nameof(column3Name));
        if (column3Color is not null) sprint.Column3Color = ColumnColor.Create(column3Color);

        sprint.AddDomainEvent(new Events.SprintCreatedEvent(sprint));
        return sprint;
    }

    public int GetTimerDefaultForPhase() => CurrentPhase switch
    {
        SprintPhase.Writing => WritingTimerDefault,
        SprintPhase.Voting => VotingTimerDefault,
        SprintPhase.Discussing => DiscussingTimerDefault,
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
        if (State != SprintState.Draft)
            throw new InvalidOperationException("Sprint must be in Draft state to start.");
        State = SprintState.Active;
        AddDomainEvent(new Events.SprintStartedEvent(this));
    }

    public void AdvancePhase()
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to advance phase.");
        CurrentPhase = CurrentPhase switch
        {
            SprintPhase.Writing => SprintPhase.Grouping,
            SprintPhase.Grouping => SprintPhase.Voting,
            SprintPhase.Voting => SprintPhase.Discussing,
            SprintPhase.Discussing => SprintPhase.Reporting,
            SprintPhase.Reporting => throw new InvalidOperationException("Sprint phase is already Reporting."),
            _ => throw new InvalidOperationException($"Unknown phase: {CurrentPhase}."),
        };
        AddDomainEvent(new Events.PhaseAdvancedEvent(this));
    }

    public void Close()
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to close.");
        State = SprintState.Closed;
        ClosedAt = DateTime.UtcNow;
        AddDomainEvent(new Events.SprintClosedEvent(this));
    }

    public void SetFocus(int groupId)
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to set focus.");
        if (CurrentPhase != SprintPhase.Discussing)
            throw new InvalidOperationException("Sprint must be in Discussing phase to set focus.");

        var previousGroupId = FocusedGroupId;
        FocusedGroupId = groupId;
        AddDomainEvent(new Events.GroupFocusedEvent(this, groupId, previousGroupId));
    }

    public void ClearFocus()
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Sprint must be Active to clear focus.");
        if (CurrentPhase != SprintPhase.Discussing)
            throw new InvalidOperationException("Sprint must be in Discussing phase to clear focus.");

        var previousGroupId = FocusedGroupId;
        FocusedGroupId = null;
        AddDomainEvent(new Events.GroupFocusClearedEvent(this, previousGroupId));
    }
}
