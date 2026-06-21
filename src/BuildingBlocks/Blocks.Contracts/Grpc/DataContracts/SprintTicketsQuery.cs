using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SprintTicketsQuery
{
    [DataMember(Order = 1)]
    public int SprintId { get; set; }
}
