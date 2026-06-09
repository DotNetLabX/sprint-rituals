using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class FindPersonByEmailResponse
{
    [DataMember(Order = 1)]
    public bool Found { get; set; }

    [DataMember(Order = 2)]
    public int PersonId { get; set; }

    [DataMember(Order = 3)]
    public string? Email { get; set; }

    [DataMember(Order = 4)]
    public string? FirstName { get; set; }

    [DataMember(Order = 5)]
    public string? LastName { get; set; }
}
