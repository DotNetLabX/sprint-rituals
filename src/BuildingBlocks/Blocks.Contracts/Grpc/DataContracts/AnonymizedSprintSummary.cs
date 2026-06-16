using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

/// <summary>
/// Team-level sprint summary with no per-developer data. Privacy boundary: §9 of the suite integration proposal.
/// </summary>
[DataContract]
public class AnonymizedSprintSummary
{
    [DataMember(Order = 1)]
    public decimal CompletionRate { get; set; }

    /// <summary>Combined scope + bug disruption rate.</summary>
    [DataMember(Order = 2)]
    public decimal DisruptionRate { get; set; }

    [DataMember(Order = 3)]
    public decimal CarryOverRate { get; set; }

    /// <summary>Composite health score 0–100.</summary>
    [DataMember(Order = 4)]
    public decimal HealthScore { get; set; }

    /// <summary>RAG classification: "green" | "amber" | "red".</summary>
    [DataMember(Order = 5)]
    public string HealthRag { get; set; } = default!;

    [DataMember(Order = 6)]
    public decimal SpCompleted { get; set; }

    [DataMember(Order = 7)]
    public decimal ActiveSp { get; set; }

    /// <summary>Top mid-sprint scope additions — ticket key, summary, and story points only. No assignee data.</summary>
    [DataMember(Order = 8)]
    public List<ScopeAdditionEntry> TopScopeAdditions { get; set; } = [];

    // --- Sprint identity (RA4 additive, Orders 9–12) ---

    /// <summary>Fokus sprint ID — identifies which sprint these metrics belong to.</summary>
    [DataMember(Order = 9)]
    public int SprintId { get; set; }

    /// <summary>Sprint name as recorded in Fokus.</summary>
    [DataMember(Order = 10)]
    public string SprintName { get; set; } = default!;

    /// <summary>Sprint start date (UTC).</summary>
    [DataMember(Order = 11)]
    public DateTime StartDate { get; set; }

    /// <summary>Sprint end date (UTC).</summary>
    [DataMember(Order = 12)]
    public DateTime EndDate { get; set; }

    // --- Per-rate RAG bands (RA4 additive, Orders 13–15) ---
    // Source: Fokus HealthScoreData — authoritative Fokus thresholds, never recomputed here (Rule 3).
    // DisruptionRag bands on the same combined DisruptionRate (Scope + Bug) as the DisruptionRate
    // field above — there is no asymmetry between the two fields.

    /// <summary>RAG classification for completion rate: "green" | "amber" | "red".</summary>
    [DataMember(Order = 13)]
    public string CompletionRag { get; set; } = default!;

    /// <summary>RAG classification for disruption rate: "green" | "amber" | "red".
    /// Bands on combined (scope + bug) disruption rate — same input as <see cref="DisruptionRate"/>.</summary>
    [DataMember(Order = 14)]
    public string DisruptionRag { get; set; } = default!;

    /// <summary>RAG classification for carry-over rate: "green" | "amber" | "red".</summary>
    [DataMember(Order = 15)]
    public string CarryOverRag { get; set; } = default!;
}

[DataContract]
public class ScopeAdditionEntry
{
    [DataMember(Order = 1)]
    public string TicketKey { get; set; } = default!;

    [DataMember(Order = 2)]
    public string Summary { get; set; } = default!;

    [DataMember(Order = 3)]
    public decimal? StoryPoints { get; set; }
}
