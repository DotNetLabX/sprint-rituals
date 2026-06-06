using Jira.Contracts;
using Mapster;
using DomainSprintState = Fokus.Domain.SprintState;

namespace Fokus.API.Features.Sync;

/// <summary>
/// ADR-006 boundary mapper: maps Jira DTOs to Fokus domain types.
/// No Jira DTO ever enters the Domain layer; all mapping happens here at the API boundary.
/// Sprint, Ticket, and Developer are registered with Mapster IRegister (clean 1:1 shapes).
/// StatusTransition and SprintMembership are plain static methods (one-to-many / multi-source).
/// </summary>
public class JiraDomainMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<(JiraSprint dto, string boardName), Sprint>()
            .MapWith(src => MapSprint(src.dto, src.boardName));

        config.NewConfig<JiraIssue, Ticket>()
            .MapWith(src => MapTicket(src));

        config.NewConfig<JiraIssue, Developer?>()
            .MapWith(src => MapDeveloper(src));
    }

    public static Sprint MapSprint(JiraSprint dto, string boardName)
    {
        var state = dto.State.ToLowerInvariant() switch
        {
            "active" => DomainSprintState.Active,
            "closed" => DomainSprintState.Closed,
            "future" => DomainSprintState.Future,
            _ => DomainSprintState.Active
        };

        return new Sprint
        {
            Id = dto.Id,
            Name = dto.Name,
            StartDate = dto.StartDate ?? DateTime.UtcNow,
            EndDate = dto.EndDate ?? DateTime.UtcNow.AddDays(14),
            BoardId = dto.OriginBoardId,
            BoardName = boardName,
            State = state,
            SyncedAt = DateTime.UtcNow,
            Goal = dto.Goal
        };
    }

    public static Ticket MapTicket(JiraIssue dto) => new()
    {
        Id = dto.Key,
        Summary = dto.Fields.Summary,
        IssueType = dto.Fields.Issuetype?.Name ?? "Unknown",
        StoryPoints = dto.Fields.StoryPoints,
        EpicKey = dto.Fields.EpicKey ?? (dto.Fields.Parent?.Fields.Issuetype?.Name == "Epic" ? dto.Fields.Parent.Key : null),
        EpicName = dto.Fields.EpicName ?? (dto.Fields.Parent?.Fields.Issuetype?.Name == "Epic" ? dto.Fields.Parent.Fields.Summary : null),
        ParentTicketKey = dto.Fields.Parent?.Fields.Issuetype?.Name != "Epic" ? dto.Fields.Parent?.Key : null,
        AssigneeId = dto.Fields.Assignee?.AccountId,
        Priority = dto.Fields.Priority?.Name ?? "Medium",
        CurrentStatus = dto.Fields.Status?.Name ?? "Unknown",
        CreatedDate = dto.Fields.Created ?? DateTime.UtcNow,
        ResolvedDate = dto.Fields.Resolutiondate
    };

    public static Developer? MapDeveloper(JiraIssue dto)
    {
        if (dto.Fields.Assignee is null) return null;

        var assignee = dto.Fields.Assignee;
        var avatarUrl = assignee.AvatarUrls.TryGetValue("48x48", out var url) ? url : null;

        return new Developer
        {
            Id = assignee.AccountId,
            DisplayName = assignee.DisplayName,
            AvatarUrl = avatarUrl,
            IsActive = true
        };
    }

    public static List<StatusTransition> MapStatusTransitions(JiraIssue dto)
    {
        var transitions = new List<StatusTransition>();

        var histories = dto.Changelog?.Histories;
        if (histories is null || histories.Count == 0) return transitions;

        foreach (var history in histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field == "status"))
            {
                transitions.Add(new StatusTransition
                {
                    TicketId = dto.Key,
                    FromStatus = item.FromString ?? string.Empty,
                    ToStatus = item.ToStringValue ?? string.Empty,
                    Timestamp = history.Created,
                    AuthorId = history.Author.AccountId
                });
            }
        }

        return transitions;
    }

    public static SprintMembership MapSprintMembership(
        JiraIssue dto,
        Sprint sprint,
        bool forcedNotCommitted = false,
        int planningWindowDays = 2)
    {
        var sprintIdStr = sprint.Id.ToString();

        DateTime? addedAt = null;
        DateTime? removedAt = null;

        var histories = dto.Changelog?.Histories;
        if (histories is not null)
        foreach (var history in histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field is "Sprint" or "sprint"))
            {
                var toSprints = item.To?.Split(',').Select(s => s.Trim()) ?? [];
                var fromSprints = item.From?.Split(',').Select(s => s.Trim()) ?? [];

                var appearsInTo = toSprints.Any(s => s == sprintIdStr);
                var appearsInFrom = fromSprints.Any(s => s == sprintIdStr);

                if (appearsInTo && !appearsInFrom && addedAt is null)
                    addedAt = history.Created;

                if (appearsInFrom && !appearsInTo && removedAt is null)
                    removedAt = history.Created;
            }
        }

        var effectiveAddedAt = addedAt ?? sprint.StartDate;
        var planningCutoff = sprint.StartDate.AddDays(planningWindowDays);
        var wasCommitted = !forcedNotCommitted && effectiveAddedAt <= planningCutoff;

        return new SprintMembership
        {
            SprintId = sprint.Id,
            TicketId = dto.Key,
            AddedAt = effectiveAddedAt,
            RemovedAt = removedAt,
            WasCommitted = wasCommitted,
            FinalStatus = dto.Fields.Status?.Name ?? "Unknown",
            StoryPoints = dto.Fields.StoryPoints
        };
    }
}
