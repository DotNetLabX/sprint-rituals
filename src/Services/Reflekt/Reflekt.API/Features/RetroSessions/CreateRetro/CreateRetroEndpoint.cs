using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.RetroSessions.CreateRetro;

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/retro-sessions")]
[Tags("RetroSessions")]
public sealed class CreateRetroSessionEndpoint(TeamRepository teamRepository, RetroSessionRepository retroSessionRepository)
    : Endpoint<CreateRetroSessionCommand, RetroSessionResponse>
{
    public override async Task HandleAsync(CreateRetroSessionCommand command, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(command.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Resolve column config: explicit values take precedence; fallback to most recent retro session in team
        string? col1Name = command.Column1Name;
        string? col1Color = command.Column1Color;
        string? col2Name = command.Column2Name;
        string? col2Color = command.Column2Color;
        string? col3Name = command.Column3Name;
        string? col3Color = command.Column3Color;

        if (col1Name is null && col1Color is null && col2Name is null && col2Color is null && col3Name is null && col3Color is null)
        {
            var previousSessions = await retroSessionRepository.GetByTeamIdAsync(command.TeamId, ct);
            var mostRecent = previousSessions.FirstOrDefault();
            if (mostRecent is not null)
            {
                col1Name = mostRecent.Column1Name;
                col1Color = mostRecent.Column1Color.Value;
                col2Name = mostRecent.Column2Name;
                col2Color = mostRecent.Column2Color.Value;
                col3Name = mostRecent.Column3Name;
                col3Color = mostRecent.Column3Color.Value;
            }
        }

        var session = RetroSession.Create(
            command.Name,
            command.TeamId,
            command.FokusSprintIds,
            command.VotesPerParticipant ?? 3,
            col1Name, col1Color,
            col2Name, col2Color,
            col3Name, col3Color,
            command.WritingTimerDefault,
            command.VotingTimerDefault,
            command.DiscussingTimerDefault);

        await retroSessionRepository.AddAsync(session, ct);
        await retroSessionRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(session.Adapt<RetroSessionResponse>(), 201, ct);
    }
}
