using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

/// <summary>
/// Scope disruption breakdown — team-level only, no per-developer data.
/// </summary>
[DataContract]
public class DisruptionMetrics
{
    [DataMember(Order = 1)]
    public decimal ScopeDisruptionRate { get; set; }

    [DataMember(Order = 2)]
    public decimal BugDisruptionRate { get; set; }

    [DataMember(Order = 3)]
    public decimal AddedSp { get; set; }

    [DataMember(Order = 4)]
    public decimal BugAddedSp { get; set; }

    [DataMember(Order = 5)]
    public int MidSprintAdditionCount { get; set; }
}
