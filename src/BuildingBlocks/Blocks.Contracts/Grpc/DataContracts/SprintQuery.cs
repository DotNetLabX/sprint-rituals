using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SprintQuery
{
    [DataMember(Order = 1)]
    public int SprintId { get; set; }

    /// <summary>
    /// Optional sub-team filter. Null or empty means all sub-teams.
    /// </summary>
    [DataMember(Order = 2)]
    public string? SubTeam { get; set; }
}
