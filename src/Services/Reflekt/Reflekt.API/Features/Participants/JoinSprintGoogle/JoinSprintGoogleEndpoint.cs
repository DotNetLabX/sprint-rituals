// RA1: unmapped pending Identity unification (RA2, ADR-016).
// JoinSprintGoogle uses Reflekt User.Id (from Google OAuth callback) — RA2 links Participant to Identity Person.
namespace Reflekt.API.Features.Participants.JoinSprintGoogle;

public sealed record JoinSprintGoogleRequest(int SprintId);

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public sealed class JoinSprintGoogleEndpoint : Endpoint<JoinSprintGoogleRequest>
{
    public override void Configure()
    {
        // RA1: unmapped pending Identity unification (RA2, ADR-016)
        Post("_ra1/unmapped/api/sprints/{sprintId}/join/google");
        DontThrowIfValidationFails();
    }

    public override Task HandleAsync(JoinSprintGoogleRequest req, CancellationToken ct) => Task.CompletedTask;
}
