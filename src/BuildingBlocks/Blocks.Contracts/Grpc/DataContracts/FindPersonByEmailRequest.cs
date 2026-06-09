using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class FindPersonByEmailRequest
{
    [DataMember(Order = 1)]
    public string Email { get; set; } = default!;
}
