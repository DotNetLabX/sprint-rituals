
namespace Reflekt.Domain.Votes;

public sealed partial class Vote : AggregateRoot
{
    private Vote() { }

    public required int GroupId { get; init; }
    public required int ParticipantId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
