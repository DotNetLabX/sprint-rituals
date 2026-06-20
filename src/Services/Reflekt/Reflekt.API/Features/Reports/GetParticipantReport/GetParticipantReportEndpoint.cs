using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Reports;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Reports.GetParticipantReport;

public sealed record GetParticipantReportRequest(int RetroSessionId);

[Authorize(Policy = AuthPolicies.OwnerOrScrumMaster)]
[HttpGet("/api/retro-sessions/{retroSessionId}/report")]
[Tags("Reports")]
public sealed class GetParticipantReportEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    ParticipantRepository participantRepository,
    RetroReportBuilder reportBuilder)
    : Endpoint<GetParticipantReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetParticipantReportRequest req, CancellationToken ct)
    {
        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var participantIdClaim = User.FindFirstValue("participantId");
        if (participantIdClaim is not null)
        {
            if (!int.TryParse(User.FindFirstValue("retroSessionId"), out var claimRetroSessionId) || claimRetroSessionId != req.RetroSessionId)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }
        else
        {
            var userId = User.GetPersonId();
            var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
            if (team is null || team.OwnerId != userId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var cards = await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var groups = await cardGroupRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var voteCounts = await voteRepository.GetVoteCountsByGroupIdsAsync(groups.Select(g => g.Id), ct);
        var participants = await participantRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var teamInfo = await teamRepository.GetByIdAsync(session.TeamId, ct);

        // Pre-load previous-actions data with short-circuit: only run the two action-item queries
        // when a previous closed session exists (preserves the original BuildPreviousActionsSectionAsync guard).
        var previousSession = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(session.TeamId, ct);
        var reviewedItems = previousSession is not null
            ? await cardRepository.GetActionItemsReviewedInRetroSessionAsync(req.RetroSessionId, ct)
            : [];
        var unreviewedEligible = previousSession is not null
            ? await cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousSession.Id, ct)
            : [];

        var result = reportBuilder.Build(
            session, cards, groups, voteCounts, participants, teamInfo!,
            reviewedItems, unreviewedEligible, previousSession);

        await Send.OkAsync(result.Adapt<ReportResponse>(), ct);
    }
}


