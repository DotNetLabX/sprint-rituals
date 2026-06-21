using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SprintTicketsResponse
{
    [DataMember(Order = 1)]
    public List<PokerTicket> Tickets { get; set; } = [];
}

[DataContract]
public class PokerTicket
{
    [DataMember(Order = 1)]
    public string Key { get; set; } = default!;

    [DataMember(Order = 2)]
    public string Summary { get; set; } = default!;

    /// <summary>
    /// Current story-points value; null if not set in Jira.
    /// </summary>
    [DataMember(Order = 3)]
    public decimal? StoryPoints { get; set; }
}
