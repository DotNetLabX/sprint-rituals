using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

/// <summary>
/// Outcome values for invitation redemption.
/// EmailMismatch is a redeem-only outcome — the validate endpoint never produces it.
/// </summary>
[DataContract]
public enum InvitationRedeemOutcome
{
    [EnumMember] Success = 0,
    [EnumMember] NotFound = 1,
    [EnumMember] Used = 2,
    [EnumMember] Revoked = 3,
    [EnumMember] Expired = 4,
    [EnumMember] EmailMismatch = 5,
}

[DataContract]
public class RedeemInvitationResponse
{
    [DataMember(Order = 1)]
    public InvitationRedeemOutcome Outcome { get; set; }

    /// <summary>The invitation's role (Admin or Manager). Populated only when Outcome == Success.</summary>
    [DataMember(Order = 2)]
    public string? Role { get; set; }

    /// <summary>The invited email address. Populated only when Outcome == Success.</summary>
    [DataMember(Order = 3)]
    public string? InvitedEmail { get; set; }
}
