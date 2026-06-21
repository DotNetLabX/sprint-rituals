using FastEndpoints;
using FluentValidation;

namespace Poker.API.Features.Sessions.JoinSession;

public class JoinSessionCommand
{
    /// <summary>Route parameter.</summary>
    public Guid Id { get; set; }

    /// <summary>Required when joining as a guest (no person_id cookie claim).</summary>
    public string? DisplayName { get; set; }
}

public class JoinSessionCommandValidator : Validator<JoinSessionCommand>
{
    public JoinSessionCommandValidator()
    {
        // DisplayName validation is deferred to the endpoint (context-dependent: not required for cookie-authed).
    }
}
