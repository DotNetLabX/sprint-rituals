using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class RedeemInvitationRequest
{
    [DataMember(Order = 1)]
    public string Token { get; set; } = default!;

    [DataMember(Order = 2)]
    public string SigningInEmail { get; set; } = default!;
}
