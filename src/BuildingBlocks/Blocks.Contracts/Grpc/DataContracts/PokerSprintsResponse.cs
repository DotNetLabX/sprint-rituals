using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class PokerSprintsResponse
{
    [DataMember(Order = 1)]
    public List<PokerSprintInfo> Sprints { get; set; } = [];
}

[DataContract]
public class PokerSprintInfo
{
    [DataMember(Order = 1)]
    public int SprintId { get; set; }

    [DataMember(Order = 2)]
    public string Name { get; set; } = default!;

    [DataMember(Order = 3)]
    public DateTime StartDate { get; set; }

    [DataMember(Order = 4)]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Jira sprint state string: "active", "future", "closed".
    /// </summary>
    [DataMember(Order = 5)]
    public string State { get; set; } = default!;
}
