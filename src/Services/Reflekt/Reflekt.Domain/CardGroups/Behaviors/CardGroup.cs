using Reflekt.Domain.Cards;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.Domain.CardGroups;

public sealed partial class CardGroup
{
    public static CardGroup Create(int retroSessionId, ColumnType columnType, string? title = null)
    {
        if (title is not null && title.Length > 100)
            throw new ArgumentException("Title must be 100 characters or fewer.", nameof(title));

        var group = new CardGroup
        {
            RetroSessionId = retroSessionId,
            ColumnType = columnType,
            Title = title,
            CreatedAt = DateTime.UtcNow,
        };
        group.AddDomainEvent(new CardGroupCreatedEvent(group));
        return group;
    }

    public void SetTitle(string? title)
    {
        if (title is not null && title.Length > 100)
            throw new ArgumentException("Title must be 100 characters or fewer.", nameof(title));

        Title = title;
        AddDomainEvent(new CardGroupTitleChangedEvent(this));
    }

    public void RecordCardAdded(int cardId, int sourceGroupId)
    {
        AddDomainEvent(new CardsGroupedEvent(this, cardId, sourceGroupId));
    }

    public void RecordCardRemoved(int cardId, int newGroupId)
    {
        AddDomainEvent(new CardUngroupedEvent(this, cardId, newGroupId));
    }

    public void MarkDiscussed()
    {
        DiscussedAt = DateTime.UtcNow;
        AddDomainEvent(new GroupDiscussedChangedEvent(this));
    }

    public void UnmarkDiscussed()
    {
        DiscussedAt = null;
        AddDomainEvent(new GroupDiscussedChangedEvent(this));
    }
}
