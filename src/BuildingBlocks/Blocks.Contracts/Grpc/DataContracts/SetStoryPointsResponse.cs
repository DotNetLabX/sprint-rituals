using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SetStoryPointsResponse
{
    /// <summary>
    /// True if the Jira write-back succeeded. False means the request was accepted
    /// by Fokus but Jira rejected the update (error details in <see cref="ErrorMessage"/>).
    /// </summary>
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    /// <summary>Populated when Success is false.</summary>
    [DataMember(Order = 2)]
    public string? ErrorMessage { get; set; }
}
