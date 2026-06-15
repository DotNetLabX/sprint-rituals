using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.PreviousActions.SetReviewStatus;

// RetroSessionId renames from SprintId in the command (Q2 route rename).
// Card.ReviewedInSprintId and Card.SetReviewStatus(int sprintId) stay SprintId-named (Q1=b DO-NOT-TOUCH).
public sealed record SetReviewStatusCommand(int TeamId, int RetroSessionId, int CardId, string ReviewStatus);

public sealed class SetReviewStatusCommandValidator : Validator<SetReviewStatusCommand>
{
    private static readonly HashSet<string> ValidStatuses =
        ["Done", "CarriedOver", "Dropped"];

    public SetReviewStatusCommandValidator()
    {
        RuleFor(x => x.ReviewStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("ReviewStatus must be one of: Done, CarriedOver, Dropped.");
    }
}

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPatch("/api/teams/{teamId}/retro-sessions/{retroSessionId}/previous-actions/{cardId}")]
[Tags("PreviousActions")]
public sealed class SetReviewStatusEndpoint(
    TeamRepository teamRepository,
    RetroSessionRepository retroSessionRepository,
    CardRepository cardRepository)
    : Endpoint<SetReviewStatusCommand, PreviousActionItemResponse>
{
    public override async Task HandleAsync(SetReviewStatusCommand command, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var team = await teamRepository.GetByIdAsync(command.TeamId, ct);
        if (team is null) { await Send.NotFoundAsync(ct); return; }

        if (team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var sprint = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (sprint is null || sprint.TeamId != command.TeamId) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State != RetroSessionState.Active || sprint.CurrentPhase != RetroPhase.Writing)
        {
            AddError("Review status can only be set during the Writing phase.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var previousSprint = await retroSessionRepository.GetMostRecentClosedByTeamIdAsync(command.TeamId, ct);
        if (previousSprint is null) { await Send.NotFoundAsync(ct); return; }

        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null || card.SprintId != previousSprint.Id)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!Enum.TryParse<Domain.Cards.ReviewStatus>(command.ReviewStatus, out var reviewStatus))
        {
            ThrowError(x => x.ReviewStatus, "ReviewStatus must be one of: Done, CarriedOver, Dropped.");
            return;
        }

        // command.RetroSessionId feeds the kept Card.ReviewedInSprintId FK column (Q1=b; domain method param stays sprintId).
        card.SetReviewStatus(reviewStatus, command.RetroSessionId);
        await cardRepository.SaveChangesAsync(ct);

        var response = new PreviousActionItemResponse(
            card.Id,
            card.Text,
            card.AssigneeName,
            card.ReviewStatus.ToString(),
            previousSprint.Id,
            previousSprint.Name);

        await Send.OkAsync(response, ct);
    }
}
