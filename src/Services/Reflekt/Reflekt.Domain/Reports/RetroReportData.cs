using Reflekt.Domain.Cards;

namespace Reflekt.Domain.Reports;

// Domain result records for report aggregation (ADR-005).
// Parallels ReportContracts.cs member-for-member but holds domain values:
// EvidenceLinkSource is the enum (stringified at the Mapster boundary).
// ReportPreviousActionData.ReviewStatus is a string because "Not reviewed" is not a ReviewStatus member.

public sealed record RetroReportData(
    ReportRetroSessionInfoData RetroSession,
    ReportTeamInfoData Team,
    IReadOnlyList<ReportParticipantInfoData> Participants,
    IReadOnlyList<ReportColumnSectionData> Columns,
    IReadOnlyList<ReportActionItemData> ActionItems,
    ReportPreviousActionsData? PreviousActionsReview = null);

public sealed record ReportRetroSessionInfoData(int Id, string Name, DateTime CreatedAt, DateTime? ClosedAt);

public sealed record ReportTeamInfoData(int Id, string Name);

public sealed record ReportParticipantInfoData(string DisplayName);

public sealed record ReportColumnSectionData(string ColumnName, string ColumnColor, IReadOnlyList<ReportGroupEntryData> Groups);

public sealed record ReportGroupEntryData(string GroupTitle, int VoteCount, bool IsDiscussed, IReadOnlyList<ReportCardEntryData> Cards);

public sealed record ReportCardEntryData(string Text, IReadOnlyList<ReportEvidenceLinkData> EvidenceLinks);

public sealed record ReportActionItemData(string Text, string? AssigneeName, IReadOnlyList<ReportEvidenceLinkData> EvidenceLinks);

// Source is the EvidenceLinkSource enum — stringified in the Mapster map at the API boundary (ADR-006).
public sealed record ReportEvidenceLinkData(string Url, EvidenceLinkSource Source, string? Title, string? ExternalKey);

public sealed record ReportPreviousActionsData(string PreviousRetroSessionName, IReadOnlyList<ReportPreviousActionData> Items);

// ReviewStatus is a string: current code emits card.ReviewStatus.ToString() for reviewed items
// and the literal "Not reviewed" for unreviewed-eligible ones. "Not reviewed" is not a ReviewStatus member,
// so the projection stays as string to preserve the exact literal.
public sealed record ReportPreviousActionData(string Text, string? AssigneeName, string ReviewStatus);
