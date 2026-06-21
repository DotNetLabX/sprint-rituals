using Microsoft.AspNetCore.Authorization;
using Poker.API.Auth;
using Poker.Persistence.Repositories;
using AuthPolicies = Poker.API.Auth.Policies;

namespace Poker.API.Features.Sessions.CreateSession;

/// <summary>
/// POST /api/poker/sessions — SM creates a new planning session.
/// 201 + room URL; 400 mode/missing-sprint/missing-title; 401 unauth (Rule 1).
/// </summary>
[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/poker/sessions")]
[Tags("PokerSessions")]
public sealed class CreateSessionEndpoint(
    PlanningSessionRepository sessionRepository)
    : Endpoint<CreateSessionCommand, CreateSessionResponse>
{
    public override async Task HandleAsync(CreateSessionCommand cmd, CancellationToken ct)
    {
        var personId = User.GetPersonId();

        var session = cmd.Mode == "SprintBound"
            ? PlanningSession.CreateSprintBound(personId, cmd.SprintId!.Value, cmd.SprintName!, cmd.Title, DateTime.UtcNow)
            : PlanningSession.CreateFree(personId, cmd.Title!, DateTime.UtcNow);

        await sessionRepository.AddAsync(session, ct);
        await sessionRepository.SaveChangesAsync(ct);

        var response = new CreateSessionResponse
        {
            SessionId = session.Id,
            RoomUrl = $"/poker/{session.Id}",
        };

        await Send.ResponseAsync(response, StatusCodes.Status201Created, ct);
    }
}
