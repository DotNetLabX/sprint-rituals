using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

/// <summary>
/// Sprint catalog entry. Note: Fokus.API.Features.Analytics also defines a SprintInfo record —
/// that type lives in a different namespace. Implementations must fully-qualify when both are in scope.
/// </summary>
[DataContract]
public class SprintInfo
{
    [DataMember(Order = 1)]
    public int SprintId { get; set; }

    [DataMember(Order = 2)]
    public string Name { get; set; } = default!;

    [DataMember(Order = 3)]
    public DateTime StartDate { get; set; }

    [DataMember(Order = 4)]
    public DateTime EndDate { get; set; }

    [DataMember(Order = 5)]
    public string State { get; set; } = default!;

    [DataMember(Order = 6)]
    public string? Goal { get; set; }
}
