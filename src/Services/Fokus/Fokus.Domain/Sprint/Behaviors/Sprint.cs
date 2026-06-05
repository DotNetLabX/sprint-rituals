namespace Fokus.Domain;

public partial class Sprint
{
    public void AddMembership(SprintMembership membership) => _memberships.Add(membership);

    public static Sprint FromJira(JiraSprint dto, string boardName)
    {
        var state = dto.State.ToLowerInvariant() switch
        {
            "active" => SprintState.Active,
            "closed" => SprintState.Closed,
            "future" => SprintState.Future,
            _ => SprintState.Active
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
}
