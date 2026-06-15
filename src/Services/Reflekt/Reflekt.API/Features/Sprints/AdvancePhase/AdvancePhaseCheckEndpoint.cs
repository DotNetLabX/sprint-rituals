using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Sprints.AdvancePhase;

public sealed record AdvancePhaseCheckResponse(
    string CurrentPhase,
    int? NonVotingParticipantCount = null,
    int? TotalParticipantCount = null,
    int? DiscussedGroupCount = null,
    int? FocusedGroupId = null,
    int? UnreviewedActionCount = null,
    int? TotalPreviousActionCount = null);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/teams/{teamId}/retro-sessions/{retroSessionId}/advance-phase/check")]
[Tags("Sprints")]
public sealed class AdvancePhaseCheckEndpoint(
    TeamRepository teamRepository,
    RetroSessionRepository retroSessionRepository,
    ParticipantRepository participantRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    CardRepository cardRepository)
    : Endpoint<RetroSessionRouteRequest, AdvancePhaseCheckResponse>
{
    public override async Task HandleAsync(RetroSessionRouteRequest request, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var sprint = await retroSessionRepository.GetByIdAsync(request.RetroSessionId, ct);
        if (sprint is null || sprint.TeamId != request.TeamId) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != RetroSessionState.Active)
        {
            AddError("Sprint must be Active to check phase advancement.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (sprint.CurrentPhase == RetroPhase.Reporting)
        {
            AddError("Cannot advance from Reporting phase.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (sprint.CurrentPhase == RetroPhase.Writing)
        {
            var previousSprint = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(sprint.TeamId, ct);
            if (previousSprint is not null)
            {
                var unreviewedItems = await cardRepository.GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct);
                var reviewedItems = await cardRepository.GetActionItemsReviewedInSprintAsync(previousSprint.Id, ct);
                var totalCount = unreviewedItems.Count + reviewedItems.Count;

                if (totalCount > 0)
                {
                    await Send.OkAsync(new AdvancePhaseCheckResponse(
                        sprint.CurrentPhase.ToString(),
                        UnreviewedActionCount: unreviewedItems.Count,
                        TotalPreviousActionCount: totalCount), ct);
                    return;
                }
            }

            await Send.OkAsync(new AdvancePhaseCheckResponse(sprint.CurrentPhase.ToString()), ct);
            return;
        }

        if (sprint.CurrentPhase == RetroPhase.Voting)
        {
            var participants = await participantRepository.GetBySprintIdAsync(request.RetroSessionId, ct);
            var participantIdsWithVotes = await voteRepository.GetParticipantIdsWithVotesInSprintAsync(request.RetroSessionId, ct);
            var votingSet = participantIdsWithVotes.ToHashSet();
            var nonVotingCount = participants.Count(p => !votingSet.Contains(p.Id));

            await Send.OkAsync(new AdvancePhaseCheckResponse(
                sprint.CurrentPhase.ToString(),
                NonVotingParticipantCount: nonVotingCount,
                TotalParticipantCount: participants.Count), ct);
            return;
        }

        if (sprint.CurrentPhase == RetroPhase.Discussing)
        {
            var groups = await cardGroupRepository.GetBySprintIdAsync(request.RetroSessionId, ct);
            var discussedCount = groups.Count(g => g.DiscussedAt.HasValue);

            await Send.OkAsync(new AdvancePhaseCheckResponse(
                sprint.CurrentPhase.ToString(),
                DiscussedGroupCount: discussedCount,
                FocusedGroupId: sprint.FocusedGroupId), ct);
            return;
        }

        await Send.OkAsync(new AdvancePhaseCheckResponse(sprint.CurrentPhase.ToString()), ct);
    }
}


