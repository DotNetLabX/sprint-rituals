namespace Fokus.API.Features.Developers.SetCapacity;

[HttpPut("/api/developers/{accountId}/capacity")]
[Tags("Developers")]
[Authorize(Roles = "Admin")]
public class SetCapacityEndpoint(DeveloperRepository developerRepository)
    : Endpoint<SetCapacityRequest, SetCapacityResponse>
{
    public override async Task HandleAsync(SetCapacityRequest req, CancellationToken ct)
    {
        var developer = await developerRepository.GetByIdAsync(req.AccountId, ct);
        if (developer is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await developerRepository.UpsertCapacityAsync(req.AccountId, req.SprintId, req.CapacityPercent, ct);
        await developerRepository.SaveChangesAsync(ct);

        await SendOkAsync(new SetCapacityResponse
        {
            DeveloperAccountId = req.AccountId,
            SprintId = req.SprintId,
            CapacityPercent = req.CapacityPercent
        }, ct);
    }
}
