using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

[ServiceContract]
public interface ISprintAnalyticsService
{
    [OperationContract]
    Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintQuery request, CancellationToken ct = default);

    [OperationContract]
    Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintQuery request, CancellationToken ct = default);

    [OperationContract]
    Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamQuery request, CancellationToken ct = default);
}
