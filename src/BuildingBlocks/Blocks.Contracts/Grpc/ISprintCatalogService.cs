using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

[ServiceContract]
public interface ISprintCatalogService
{
    [OperationContract]
    Task<SprintCatalogResponse> GetAvailableSprintsAsync(TeamQuery request, CancellationToken ct = default);
}
