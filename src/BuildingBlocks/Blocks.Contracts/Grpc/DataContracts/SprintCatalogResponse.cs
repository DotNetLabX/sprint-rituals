using System.Runtime.Serialization;

namespace Blocks.Contracts.Grpc.DataContracts;

[DataContract]
public class SprintCatalogResponse
{
    [DataMember(Order = 1)]
    public List<SprintInfo> Sprints { get; set; } = [];
}
