namespace Fokus.API.Features.Settings.SaveBoard;

public class SaveBoardCommand
{
    public int? BoardId { get; set; }
}

public class SaveBoardCommandValidator : Validator<SaveBoardCommand>
{
    public SaveBoardCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .When(x => x.BoardId.HasValue)
            .WithMessage("Board ID must be greater than 0.");
    }
}

public class SaveBoardResponse
{
    public bool Success { get; set; }
}
