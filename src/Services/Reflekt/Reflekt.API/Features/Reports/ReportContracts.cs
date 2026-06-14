namespace Reflekt.API.Features.Reports;

public sealed record ReportResponse(
    ReportSprintInfo Sprint,
    ReportTeamInfo Team,
    List<ReportParticipantInfo> Participants,
    List<ReportColumnSection> Columns,
    List<ReportActionItemEntry> ActionItems,
    ReportPreviousActionsSection? PreviousActionsReview = null);

public sealed record ReportSprintInfo(int Id, string Name, DateTime CreatedAt, DateTime? ClosedAt);

public sealed record ReportTeamInfo(int Id, string Name);

public sealed record ReportParticipantInfo(string DisplayName);

public sealed record ReportColumnSection(string ColumnName, string ColumnColor, List<ReportGroupEntry> Groups);

public sealed record ReportGroupEntry(string GroupTitle, int VoteCount, bool IsDiscussed, List<ReportCardEntry> Cards);

public sealed record ReportCardEntry(string Text, List<ReportEvidenceLinkEntry> EvidenceLinks);

public sealed record ReportActionItemEntry(string Text, string? AssigneeName, List<ReportEvidenceLinkEntry> EvidenceLinks);

public sealed record ReportEvidenceLinkEntry(string Url, string Source, string? Title, string? ExternalKey);

public sealed record ReportPreviousActionsSection(string PreviousSprintName, List<ReportPreviousActionEntry> Items);

public sealed record ReportPreviousActionEntry(string Text, string? AssigneeName, string ReviewStatus);
