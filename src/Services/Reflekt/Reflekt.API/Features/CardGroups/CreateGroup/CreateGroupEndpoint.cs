using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.RetroSessions;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

public sealed record CreateGroupRequest(int RetroSessionId, List<int> CardIds, string? Title);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/retro-sessions/{retroSessionId}/card-groups")]
[Tags("CardGroups")]
public sealed class CreateGroupEndpoint(
    RetroSessionRepository retroSessionRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<RetroHub> hubContext)
    : Endpoint<CreateGroupRequest, CardGroupResponse>
{
    public override async Task HandleAsync(CreateGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetPersonId();

        var session = await retroSessionRepository.GetByIdAsync(req.RetroSessionId, ct);
        if (session is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(session.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(session.CurrentPhase, RetroAction.GroupCards))
            ThrowError($"{RetroAction.GroupCards} is not allowed in phase {session.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (req.CardIds.Count < 2)
            ThrowError("At least 2 cards are required to create a group.", StatusCodes.Status400BadRequest);

        var cards = await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
        var selectedCards = cards.Where(c => req.CardIds.Contains(c.Id)).ToList();

        if (selectedCards.Count != req.CardIds.Count)
            ThrowError("One or more cards not found in this retro.", StatusCodes.Status400BadRequest);

        if (selectedCards.Any(c => c.CardType != CardType.Note))
            ThrowError("Only Note cards can be grouped.", StatusCodes.Status400BadRequest);

        var columnTypes = selectedCards.Select(c => c.ColumnType).Distinct().ToList();
        if (columnTypes.Count > 1)
            ThrowError("All cards must be in the same column.", StatusCodes.Status400BadRequest);

        var newGroup = CardGroup.Create(req.RetroSessionId, columnTypes[0], req.Title);
        newGroup.ClearDomainEvents();
        await cardGroupRepository.AddAsync(newGroup, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        var sourceGroupIds = selectedCards
            .Where(c => c.GroupId.HasValue)
            .Select(c => c.GroupId!.Value)
            .Distinct()
            .ToList();

        foreach (var card in selectedCards)
            card.SetGroupId(newGroup.Id);

        var emptySourceGroups = new List<CardGroup>();
        if (sourceGroupIds.Count > 0)
        {
            var allSessionCards = await cardRepository.GetByRetroSessionIdAsync(req.RetroSessionId, ct);
            foreach (var sourceId in sourceGroupIds)
            {
                if (!allSessionCards.Any(c => c.GroupId == sourceId))
                {
                    var sourceGroup = await cardGroupRepository.GetByIdAsync(sourceId, ct);
                    if (sourceGroup is not null)
                        emptySourceGroups.Add(sourceGroup);
                }
            }
        }

        foreach (var empty in emptySourceGroups)
            cardGroupRepository.Remove(empty);

        await cardGroupRepository.SaveChangesAsync(ct);

        await hubContext.Clients.Group($"retro:{req.RetroSessionId}")
            .SendAsync("GroupCreated", new
            {
                group = new
                {
                    id = newGroup.Id,
                    retroSessionId = newGroup.RetroSessionId,
                    columnType = newGroup.ColumnType.ToString(),
                    title = newGroup.Title,
                    cardIds = selectedCards.Select(c => c.Id).ToList()
                }
            }, ct);

        await Send.ResponseAsync(newGroup.Adapt<CardGroupResponse>() with
        {
            Cards = selectedCards.Select(c => c.Adapt<CardGroupCardEntry>()).ToList(),
            IsFocused = session.FocusedGroupId == newGroup.Id
        }, 201, ct);
    }
}


