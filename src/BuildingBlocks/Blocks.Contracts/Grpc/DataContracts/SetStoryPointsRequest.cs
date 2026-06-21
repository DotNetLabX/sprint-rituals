using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SetStoryPointsRequest
{
    [DataMember(Order = 1)]
    public string IssueKey { get; set; } = default!;

    [DataMember(Order = 2)]
    public decimal StoryPoints { get; set; }
}
