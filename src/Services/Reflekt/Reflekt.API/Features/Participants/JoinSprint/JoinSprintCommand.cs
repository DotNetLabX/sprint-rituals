using FluentValidation;

namespace Reflekt.API.Features.Participants.JoinSprint;

public sealed record JoinSprintCommand(int SprintId, string DisplayName);

public sealed class JoinSprintCommandValidator : Validator<JoinSprintCommand>
{
    public JoinSprintCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(50);
    }
}
