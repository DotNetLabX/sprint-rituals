namespace Fokus.API.Features.Settings.SaveBoard;

public class SaveBoardRequest
{
    public int? BoardId { get; set; }
}

public class SaveBoardRequestValidator : Validator<SaveBoardRequest>
{
    public SaveBoardRequestValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .When(x => x.BoardId.HasValue)
            .WithMessage("Board ID must be greater than 0.");
    }
}

[HttpPut("/api/settings/board")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveBoardEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveBoardRequest, SaveBoardResponse>
{
    public override async Task HandleAsync(SaveBoardRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.BoardId = req.BoardId;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveBoardResponse { Success = true }, ct);
    }
}

public class SaveBoardResponse
{
    public bool Success { get; set; }
}
