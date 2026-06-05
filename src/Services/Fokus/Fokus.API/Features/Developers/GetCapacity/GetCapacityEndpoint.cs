namespace Fokus.API.Features.Developers.GetCapacity;

[HttpGet("/api/developers/{accountId}/capacity")]
[Tags("Developers")]
public class GetCapacityEndpoint(DeveloperRepository developerRepository)
    : Endpoint<GetCapacityRequest, List<CapacityEntry>>
{
    public override async Task HandleAsync(GetCapacityRequest req, CancellationToken ct)
    {
        var developer = await developerRepository.GetByIdAsync(req.AccountId, ct);
        if (developer is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var capacities = await developerRepository.GetCapacitiesForDeveloperAsync(req.AccountId, req.SprintId, ct);

        var entries = capacities
            .Select(c => new CapacityEntry { SprintId = c.SprintId, CapacityPercent = c.CapacityPercent })
            .ToList();

        await SendOkAsync(entries, ct);
    }
}
