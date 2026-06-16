using Microsoft.AspNetCore.Authorization;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.RetroSessions.GetSprintContext;

/// <summary>
/// Assembles the sprint context for a retro session by pulling each referenced Fokus sprint's
/// anonymized summary over gRPC, applying a per-sprint snapshot fallback when Fokus is unreachable.
/// Open-participation endpoint (ADR-021 / AllowAnonymous).
/// Assembly logic lives in <see cref="GetSprintContextAssembler"/> for testability.
/// </summary>
[AllowAnonymous]
[HttpGet("/api/retro-sessions/{retroSessionId}/sprint-context")]
[Tags("RetroSessions")]
public sealed class GetSprintContextEndpoint(
    RetroSessionRepository retroSessionRepository,
    GetSprintContextAssembler assembler)
    : Endpoint<SprintContextRequest, SprintContextResponse>
{
    public override async Task HandleAsync(SprintContextRequest request, CancellationToken ct)
    {
        var retro = await retroSessionRepository.GetWithFokusSprintsAsync(request.RetroSessionId, ct);
        if (retro is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var sprintIds = retro.FokusSprintRefs.Select(r => r.FokusSprintId);
        var sections = await assembler.BuildSectionsAsync(sprintIds, ct);
        await Send.OkAsync(new SprintContextResponse(sections), ct);
    }
}
