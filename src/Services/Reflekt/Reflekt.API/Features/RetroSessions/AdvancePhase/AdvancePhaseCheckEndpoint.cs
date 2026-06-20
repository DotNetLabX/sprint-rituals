using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.RetroSessions.AdvancePhase;

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
[Tags("RetroSessions")]
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

        var session = await retroSessionRepository.GetByIdAsync(request.RetroSessionId, ct);
        if (session is null || session.TeamId != request.TeamId) { await Send.NotFoundAsync(ct); return; }

        if (session.State != RetroSessionState.Active)
        {
            AddError("Retro must be Active to check phase advancement.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (session.CurrentPhase == RetroPhase.Reporting)
        {
            AddError("Cannot advance from Reporting phase.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        if (session.CurrentPhase == RetroPhase.Writing)
        {
            var previousSession = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(session.TeamId, ct);
            if (previousSession is not null)
            {
                var unreviewedItems = await cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousSession.Id, ct);
                var reviewedItems = await cardRepository.GetActionItemsReviewedInRetroSessionAsync(previousSession.Id, ct);
                var totalCount = unreviewedItems.Count + reviewedItems.Count;

                if (totalCount > 0)
                {
                    await Send.OkAsync(new AdvancePhaseCheckResponse(
                        session.CurrentPhase.ToString(),
                        UnreviewedActionCount: unreviewedItems.Count,
                        TotalPreviousActionCount: totalCount), ct);
                    return;
                }
            }

            await Send.OkAsync(new AdvancePhaseCheckResponse(session.CurrentPhase.ToString()), ct);
            return;
        }

        if (session.CurrentPhase == RetroPhase.Voting)
        {
            var participants = await participantRepository.GetByRetroSessionIdAsync(request.RetroSessionId, ct);
            var participantIdsWithVotes = await voteRepository.GetParticipantIdsWithVotesForRetroSessionAsync(request.RetroSessionId, ct);
            var votingSet = participantIdsWithVotes.ToHashSet();
            var nonVotingCount = participants.Count(p => !votingSet.Contains(p.Id));

            await Send.OkAsync(new AdvancePhaseCheckResponse(
                session.CurrentPhase.ToString(),
                NonVotingParticipantCount: nonVotingCount,
                TotalParticipantCount: participants.Count), ct);
            return;
        }

        if (session.CurrentPhase == RetroPhase.Discussing)
        {
            var groups = await cardGroupRepository.GetByRetroSessionIdAsync(request.RetroSessionId, ct);
            var discussedCount = groups.Count(g => g.DiscussedAt.HasValue);

            await Send.OkAsync(new AdvancePhaseCheckResponse(
                session.CurrentPhase.ToString(),
                DiscussedGroupCount: discussedCount,
                FocusedGroupId: session.FocusedGroupId), ct);
            return;
        }

        await Send.OkAsync(new AdvancePhaseCheckResponse(session.CurrentPhase.ToString()), ct);
    }
}


