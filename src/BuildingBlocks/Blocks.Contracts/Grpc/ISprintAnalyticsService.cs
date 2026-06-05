using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

[ServiceContract]
public interface ISprintAnalyticsService
{
    [OperationContract]
    Task<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintRequest request, CancellationToken ct = default);

    [OperationContract]
    Task<DisruptionMetrics> GetDisruptionMetricsAsync(SprintRequest request, CancellationToken ct = default);

    [OperationContract]
    Task<CarryOverHistory> GetCarryOverHistoryAsync(TeamRequest request, CancellationToken ct = default);
}
