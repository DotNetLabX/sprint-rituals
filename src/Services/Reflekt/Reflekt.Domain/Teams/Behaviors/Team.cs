namespace Reflekt.Domain.Teams;

public sealed partial class Team
{
    public static Team Create(string name, int ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var team = new Team
        {
            Name = name,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
        };

        team.AddDomainEvent(new Events.TeamCreatedEvent(team));
        return team;
    }
}
