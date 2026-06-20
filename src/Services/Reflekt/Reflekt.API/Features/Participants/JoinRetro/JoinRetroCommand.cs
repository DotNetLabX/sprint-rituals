using FluentValidation;

namespace Reflekt.API.Features.Participants.JoinRetro;

// DisplayName is optional: authenticated suite members join by their Identity name (server-derived
// from the cookie's ClaimTypes.Name) and send no body; guests must supply one — the presence rule is
// enforced in the handler, the only place the cookie (guest vs suite member) is visible.
public sealed record JoinRetroCommand(int RetroSessionId, string? DisplayName);

public sealed class JoinRetroCommandValidator : Validator<JoinRetroCommand>
{
    public JoinRetroCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    }
}
