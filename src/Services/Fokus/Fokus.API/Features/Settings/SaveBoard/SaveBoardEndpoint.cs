namespace Fokus.API.Features.Settings.SaveBoard;

[HttpPut("/api/settings/board")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveBoardEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveBoardCommand, SaveBoardResponse>
{
    public override async Task HandleAsync(SaveBoardCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        req.Adapt(existing);
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveBoardResponse { Success = true }, ct);
    }
}
