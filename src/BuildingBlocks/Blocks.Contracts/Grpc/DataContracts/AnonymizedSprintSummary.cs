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
