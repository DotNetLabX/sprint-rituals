using Reflekt.Domain.Cards;

namespace Reflekt.Domain.CardGroups;

public sealed partial class CardGroup : AggregateRoot
{
    private CardGroup() { }

    public required int SprintId { get; init; }
    public required ColumnType ColumnType { get; init; }
    public string? Title { get; private set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? DiscussedAt { get; private set; }
}
