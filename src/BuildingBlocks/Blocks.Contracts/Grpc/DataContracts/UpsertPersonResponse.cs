using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class UpsertPersonResponse
{
    [DataMember(Order = 1)]
    public int PersonId { get; set; }
}
