namespace Fokus.API.Features.Analytics;

// Shared wire records consumed by more than one endpoint (Q1 → Option B).
// Each record is declared exactly once here. Endpoint-local wire files must NOT re-declare these.
//
// Domain counterparts (domain layer — ADR-006):
//   SprintSummaryItem           → SprintSummaryItemData       (DeveloperThroughputResult.cs)
//   DeveloperProgressSprintInfo → DeveloperProgressSprintData (DeveloperProgressResult.cs)
//   CompletedTicketEntry        → CompletedTicketData         (DeveloperProgressResult.cs)
//   DayBreakdownEntry           → DayBreakdownData            (DeveloperProgressResult.cs)
//   StalledTicketEntry          → StalledTicketData           (DeveloperProgressResult.cs)

public record SprintSummaryItem(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record DeveloperProgressSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record CompletedTicketEntry(string Key, string Summary, decimal? StoryPoints, string IssueType);

public record DayBreakdownEntry(int Day, DateTime Date, List<CompletedTicketEntry> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp);

public record StalledTicketEntry(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition);
