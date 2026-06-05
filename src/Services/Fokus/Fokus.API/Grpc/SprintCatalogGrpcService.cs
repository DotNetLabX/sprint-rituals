using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Fokus.Persistence;
using ProtoBuf.Grpc;

namespace Fokus.API.Grpc;

/// <summary>
/// gRPC server implementation providing the sprint catalog — list of available sprints for cross-module queries.
/// </summary>
public class SprintCatalogGrpcService(SprintRepository sprintRepository) : ISprintCatalogService
{
    public async Task<SprintCatalogResponse> GetAvailableSprintsAsync(TeamRequest request, CancellationToken ct = default)
    {
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        var sprintInfos = sprints
            .Select(s => new SprintInfo
            {
                SprintId = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                State = s.State.ToString(),
                Goal = s.Goal,
            })
            .ToList();

        return new SprintCatalogResponse { Sprints = sprintInfos };
    }
}
