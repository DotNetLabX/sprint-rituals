using FluentValidation;

namespace Reflekt.API.Features.Participants.JoinRetro;

public sealed record JoinRetroCommand(int RetroSessionId, string DisplayName);

public sealed class JoinRetroCommandValidator : Validator<JoinRetroCommand>
{
    public JoinRetroCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(50);
    }
}
