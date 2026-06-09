using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class UpsertPersonRequest
{
    /// <summary>External system ID (e.g. Jira AccountId). Used as match key for Jira-sourced Persons.</summary>
    [DataMember(Order = 1)]
    public string? ExternalId { get; set; }

    /// <summary>Source of this person record (e.g. "Jira", "Google").</summary>
    [DataMember(Order = 2)]
    public string Source { get; set; } = default!;

    /// <summary>Email. Nullable — Jira Cloud does not expose developer email by default.</summary>
    [DataMember(Order = 3)]
    public string? Email { get; set; }

    [DataMember(Order = 4)]
    public string FirstName { get; set; } = default!;

    [DataMember(Order = 5)]
    public string LastName { get; set; } = default!;

    [DataMember(Order = 6)]
    public string? PictureUrl { get; set; }
}
