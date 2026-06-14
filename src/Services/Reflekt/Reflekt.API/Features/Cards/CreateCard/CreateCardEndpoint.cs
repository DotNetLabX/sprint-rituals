using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.CreateCard;

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/sprints/{sprintId}/cards")]
[Tags("Cards")]
public sealed class CreateCardEndpoint(SprintRepository sprintRepository, CardRepository cardRepository, PhaseActionCache phaseActionCache)
    : Endpoint<CreateCardCommand, CardResponse>
{
    public override async Task HandleAsync(CreateCardCommand command, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sprintId"), out var claimSprintId) || claimSprintId != command.SprintId)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(User.FindFirstValue("participantId"), out var participantId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!Enum.TryParse<ColumnType>(command.ColumnType, out var columnType))
        {
            ThrowError(x => x.ColumnType, "Invalid column type.");
            return;
        }

        if (!Enum.TryParse<CardType>(command.CardType, out var cardType))
        {
            ThrowError(x => x.CardType, "Invalid card type.");
            return;
        }

        var sprint = await sprintRepository.GetByIdAsync(command.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        if (sprint.State == SprintState.Closed)
            ThrowError("Sprint is closed.", StatusCodes.Status409Conflict);

        var action = cardType == CardType.ActionItem ? SprintAction.CreateActionItem : SprintAction.CreateNote;
        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, action))
            ThrowError($"{action} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (sprint.CurrentPhase == SprintPhase.Discussing && cardType == CardType.ActionItem)
        {
            var cookieResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            if (cookieResult.Principal?.FindFirstValue(ClaimTypes.Email) is null)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var card = Card.Create(command.Text, command.SprintId, participantId, columnType, cardType, command.AssigneeName);

        await cardRepository.AddAsync(card, ct);
        await cardRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(card.Adapt<CardResponse>(), 201, ct);
    }
}
