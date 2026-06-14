using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Features.PhaseActions;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

public sealed record CreateGroupRequest(int SprintId, List<int> CardIds, string? Title);

[Authorize(Policy = AuthPolicies.ScrumMaster)]
[HttpPost("/api/sprints/{sprintId}/card-groups")]
[Tags("CardGroups")]
public sealed class CreateGroupEndpoint(
    SprintRepository sprintRepository,
    TeamRepository teamRepository,
    CardRepository cardRepository,
    CardGroupRepository cardGroupRepository,
    PhaseActionCache phaseActionCache,
    IHubContext<SprintHub> hubContext)
    : Endpoint<CreateGroupRequest, CardGroupResponse>
{
    public override async Task HandleAsync(CreateGroupRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null) { await Send.NotFoundAsync(ct); return; }

        var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
        if (team is null || team.OwnerId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.GroupCards))
            ThrowError($"{SprintAction.GroupCards} is not allowed in phase {sprint.CurrentPhase}.", StatusCodes.Status409Conflict);

        if (req.CardIds.Count < 2)
            ThrowError("At least 2 cards are required to create a group.", StatusCodes.Status400BadRequest);

        var cards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
        var selectedCards = cards.Where(c => req.CardIds.Contains(c.Id)).ToList();

        if (selectedCards.Count != req.CardIds.Count)
            ThrowError("One or more cards not found in this sprint.", StatusCodes.Status400BadRequest);

        if (selectedCards.Any(c => c.CardType != CardType.Note))
            ThrowError("Only Note cards can be grouped.", StatusCodes.Status400BadRequest);

        var columnTypes = selectedCards.Select(c => c.ColumnType).Distinct().ToList();
        if (columnTypes.Count > 1)
            ThrowError("All cards must be in the same column.", StatusCodes.Status400BadRequest);

        var newGroup = CardGroup.Create(req.SprintId, columnTypes[0], req.Title);
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
            var allSprintCards = await cardRepository.GetBySprintIdAsync(req.SprintId, ct);
            foreach (var sourceId in sourceGroupIds)
            {
                if (!allSprintCards.Any(c => c.GroupId == sourceId))
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

        await hubContext.Clients.Group($"sprint:{req.SprintId}")
            .SendAsync("GroupCreated", new
            {
                group = new
                {
                    id = newGroup.Id,
                    sprintId = newGroup.SprintId,
                    columnType = newGroup.ColumnType.ToString(),
                    title = newGroup.Title,
                    cardIds = selectedCards.Select(c => c.Id).ToList()
                }
            }, ct);

        await Send.ResponseAsync(newGroup.Adapt<CardGroupResponse>() with
        {
            Cards = selectedCards.Select(c => c.Adapt<CardGroupCardEntry>()).ToList(),
            IsFocused = sprint.FocusedGroupId == newGroup.Id
        }, 201, ct);
    }
}
