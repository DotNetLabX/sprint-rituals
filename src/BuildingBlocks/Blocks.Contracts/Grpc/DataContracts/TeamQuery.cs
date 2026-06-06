using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class TeamQuery
{
    /// <summary>
    /// Optional sub-team filter. Null or empty means all sub-teams.
    /// </summary>
    [DataMember(Order = 1)]
    public string? SubTeam { get; set; }
}
