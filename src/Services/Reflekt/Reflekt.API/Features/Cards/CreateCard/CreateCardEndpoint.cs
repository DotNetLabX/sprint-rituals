using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Cards.CreateCard;

[Authorize(Policy = AuthPolicies.Participant)]
[HttpPost("/api/retro-sessions/{retroSessionId}/cards")]
[Tags("Cards")]
public sealed class CreateCardEndpoint(RetroSessionRepository retroSessionRepository, CardRepository cardRepository, PhaseActionCache phaseActionCache)
    : Endpoint<CreateCardCommand, CardResponse>
{
    public override async Task HandleAsync(CreateCardCommand command, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("retroSessionId"), out var claimRetroSessionId) || claimRetroSessionId != command.RetroSessionId)
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

        var session = await retroSessionRepository.GetByIdAsync(command.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        if (session.State == RetroSessionState.Closed)
            ThrowError("Retro is closed.", StatusCodes.Status409Conflict);

        var action = cardType == CardType.ActionItem ? RetroAction.CreateActionItem : RetroAction.CreateNote;
        if (!phaseActionCache.IsAllowed(session.CurrentPhase, action))
            ThrowError($"{action} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (session.CurrentPhase == RetroPhase.Discussing && cardType == CardType.ActionItem)
        {
            var cookieResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            if (cookieResult.Principal?.FindFirstValue(ClaimTypes.Email) is null)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
        }

        var card = Card.Create(command.Text, command.RetroSessionId, participantId, columnType, cardType, command.AssigneeName);

        await cardRepository.AddAsync(card, ct);
        await cardRepository.SaveChangesAsync(ct);

        await Send.ResponseAsync(card.Adapt<CardResponse>(), 201, ct);
    }
}


