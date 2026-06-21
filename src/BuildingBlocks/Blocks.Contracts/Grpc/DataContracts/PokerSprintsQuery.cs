using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class PokerSprintsQuery
{
    /// <summary>
    /// Number of past-Closed sprints to include. Defaults to 3 if not set.
    /// Active and Future sprints are always included.
    /// </summary>
    [DataMember(Order = 1)]
    public int RecentClosedCount { get; set; } = 3;
}
