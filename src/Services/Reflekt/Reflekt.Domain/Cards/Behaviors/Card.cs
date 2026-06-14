using Reflekt.Domain.Cards.Events;

namespace Reflekt.Domain.Cards;


public sealed partial class Card
{
    public static Card Create(
        string text,
        int sprintId,
        int participantId,
        ColumnType columnType,
        CardType cardType,
        string? assigneeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (cardType == CardType.ActionItem)
        {
            if (string.IsNullOrWhiteSpace(assigneeName))
                throw new ArgumentException("ActionItem cards require an assignee name.", nameof(assigneeName));
            if (columnType != ColumnType.ActionItems)
                throw new ArgumentException("ActionItem cards must be in the ActionItems column.", nameof(columnType));
        }

        if (cardType == CardType.Note)
        {
            if (columnType == ColumnType.ActionItems)
                throw new ArgumentException("Note cards cannot be placed in the ActionItems column.", nameof(columnType));
            if (assigneeName is not null)
                throw new ArgumentException("Note cards cannot have an assignee.", nameof(assigneeName));
        }

        var now = DateTime.UtcNow;
        var card = new Card
        {
            SprintId = sprintId,
            ParticipantId = participantId,
            ColumnType = columnType,
            CardType = cardType,
            Text = text,
            AssigneeName = assigneeName,
            CreatedAt = now,
            UpdatedAt = now,
        };
        card.AddDomainEvent(new CardCreatedEvent(card));
        return card;
    }

    public void Update(string text, string? assigneeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (CardType == CardType.ActionItem && string.IsNullOrWhiteSpace(assigneeName))
            throw new ArgumentException("ActionItem cards require an assignee name.", nameof(assigneeName));
        if (CardType == CardType.Note && assigneeName is not null)
            throw new ArgumentException("Note cards cannot have an assignee.", nameof(assigneeName));

        Text = text;
        AssigneeName = assigneeName;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CardUpdatedEvent(this));
    }

    public void SetGroupId(int? groupId)
    {
        GroupId = groupId;
    }

    public void MarkDeleted()
    {
        AddDomainEvent(new CardDeletedEvent(this));
    }

    public EvidenceLink AttachEvidenceLink(string url, EvidenceLinkSource source, string? externalKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var link = EvidenceLink.Create(Id, url, source, externalKey);
        _evidenceLinks.Add(link);
        AddDomainEvent(new EvidenceLinkAttachedEvent(this, link));
        return link;
    }

    public void UpdateEvidenceLinkMetadata(int evidenceLinkId, string? title, string? description, string? iconUrl, string? imageUrl, FetchStatus status)
    {
        var link = _evidenceLinks.FirstOrDefault(e => e.Id == evidenceLinkId)
            ?? throw new InvalidOperationException($"EvidenceLink {evidenceLinkId} not found on card {Id}.");

        link.UpdateMetadata(title, description, iconUrl, imageUrl, status);
        AddDomainEvent(new EvidenceLinkFetchedEvent(this, link));
    }

    public void DetachEvidenceLink(int evidenceLinkId)
    {
        var link = _evidenceLinks.FirstOrDefault(e => e.Id == evidenceLinkId)
            ?? throw new InvalidOperationException($"EvidenceLink {evidenceLinkId} not found on card {Id}.");

        _evidenceLinks.Remove(link);
        AddDomainEvent(new EvidenceLinkDetachedEvent(this, evidenceLinkId));
    }

    public void SetReviewStatus(ReviewStatus status, int reviewingSprintId)
    {
        if (status == ReviewStatus.None)
            throw new ArgumentException("Cannot set review status to None. Use Done, CarriedOver, or Dropped.", nameof(status));

        if (CardType != CardType.ActionItem)
            throw new InvalidOperationException("Only ActionItem cards can have a review status.");

        if (ReviewStatus == status && ReviewedInSprintId == reviewingSprintId)
            return;

        ReviewStatus = status;
        ReviewedInSprintId = reviewingSprintId;
        AddDomainEvent(new ReviewStatusChangedEvent(this));
    }
}
