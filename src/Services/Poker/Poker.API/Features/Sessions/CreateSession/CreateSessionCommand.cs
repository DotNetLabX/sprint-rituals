using FastEndpoints;
using FluentValidation;

namespace Poker.API.Features.Sessions.CreateSession;

public class CreateSessionCommand
{
    /// <summary>"SprintBound" | "Free"</summary>
    public string Mode { get; set; } = default!;

    /// <summary>Required in Sprint-bound mode.</summary>
    public int? SprintId { get; set; }

    /// <summary>Snapshot of sprint name (Sprint-bound only, resolved from the gRPC sprint catalog).</summary>
    public string? SprintName { get; set; }

    /// <summary>Human label; required in Free mode, optional in Sprint-bound.</summary>
    public string? Title { get; set; }
}

public class CreateSessionResponse
{
    public Guid SessionId { get; set; }
    public string RoomUrl { get; set; } = default!;
}

public class CreateSessionCommandValidator : Validator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Mode).NotEmpty().Must(m => m is "SprintBound" or "Free")
            .WithMessage("Mode must be 'SprintBound' or 'Free'.");

        When(x => x.Mode == "SprintBound", () =>
        {
            RuleFor(x => x.SprintId).NotNull().WithMessage("SprintId is required for Sprint-bound sessions.");
            RuleFor(x => x.SprintName).NotEmpty().WithMessage("SprintName is required for Sprint-bound sessions.");
        });

        When(x => x.Mode == "Free", () =>
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required for Free-mode sessions.");
        });
    }
}
