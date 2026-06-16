using Fokus.Domain;

namespace Fokus.API.Tests._Fixtures;

/// <summary>
/// Static factory methods for domain objects used in the Fokus API test suite.
/// Returns valid defaults; override specific fields via named params.
/// </summary>
public static class TestData
{
    public static Sprint ClosedSprint(
        int id = 1,
        string name = "Sprint 1",
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? start.AddDays(14);
        return new Sprint
        {
            Id = id,
            Name = name,
            StartDate = start,
            EndDate = end,
            BoardId = 1,
            BoardName = "Test Board",
            State = SprintState.Closed,
            SyncedAt = end.AddDays(1),
        };
    }

    public static Ticket FeatureTicket(string id = "FOK-1", string summary = "A feature")
        => new()
        {
            Id = id,
            Summary = summary,
            IssueType = "Story",
            Priority = "Medium",
            CurrentStatus = "Done",
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    /// <summary>
    /// A membership added at sprint start (committed scope — not a mid-sprint addition).
    /// PlanningWindowDays default is 2, so AddedAt must be &lt;= StartDate + 2d.
    /// </summary>
    public static SprintMembership CommittedMembership(
        int sprintId,
        string ticketId = "FOK-1",
        decimal storyPoints = 3m,
        string finalStatus = "Done",
        Ticket? ticket = null,
        DateTime? sprintStart = null)
    {
        var start = sprintStart ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new SprintMembership
        {
            SprintId = sprintId,
            TicketId = ticketId,
            // Added on day 1 — within planning window, so not counted as a mid-sprint addition
            AddedAt = start,
            FinalStatus = finalStatus,
            StoryPoints = storyPoints,
            WasCommitted = true,
            Ticket = ticket ?? FeatureTicket(ticketId),
        };
    }

    public static StatusTransition DoneTransition(
        string ticketId,
        int sprintId,
        DateTime? timestamp = null)
    {
        return new StatusTransition
        {
            TicketId = ticketId,
            FromStatus = "In Progress",
            ToStatus = "Done",
            Timestamp = timestamp ?? new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
