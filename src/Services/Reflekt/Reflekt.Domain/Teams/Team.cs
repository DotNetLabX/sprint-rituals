
namespace Reflekt.Domain.Teams;

public sealed partial class Team : AggregateRoot
{
    private Team() { }

    public required string Name { get; init; }
    public required int OwnerId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
