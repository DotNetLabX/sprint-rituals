using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

/// <summary>
/// Carry-over metrics for the current sprint plus a rolling history — team-level only.
/// </summary>
[DataContract]
public class CarryOverHistory
{
    [DataMember(Order = 1)]
    public decimal CarryOverRate { get; set; }

    [DataMember(Order = 2)]
    public decimal CarryOverSp { get; set; }

    [DataMember(Order = 3)]
    public int ZombieTicketCount { get; set; }

    /// <summary>Up to last 4 closed sprints, ascending.</summary>
    [DataMember(Order = 4)]
    public List<SprintCarryOverEntry> SprintHistory { get; set; } = [];
}

[DataContract]
public class SprintCarryOverEntry
{
    [DataMember(Order = 1)]
    public string SprintName { get; set; } = default!;

    [DataMember(Order = 2)]
    public decimal CarryOverRate { get; set; }
}
