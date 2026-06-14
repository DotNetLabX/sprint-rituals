using Microsoft.AspNetCore.Authorization;

namespace Reflekt.API.Features.PhaseActions.GetPhaseActions;

[AllowAnonymous]
[HttpGet("/api/phase-actions")]
[Tags("PhaseActions")]
public sealed class GetPhaseActionsEndpoint(PhaseActionCache phaseActionCache)
    : EndpointWithoutRequest<List<PhaseActionsEntry>>
{
    public override Task HandleAsync(CancellationToken ct)
    {
        var matrix = phaseActionCache.GetMatrix();
        var response = matrix
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new PhaseActionsEntry(kvp.Key.ToString(), kvp.Value.Select(a => a.ToString()).ToList()))
            .ToList();
        return Send.OkAsync(response, ct);
    }
}
