
namespace Reflekt.Domain.Cards;

public sealed partial class Card : AggregateRoot
{
    private Card() { }

    public required int SprintId { get; init; }
    public required int ParticipantId { get; init; }
    public required ColumnType ColumnType { get; init; }
    public required CardType CardType { get; init; }
    public string Text { get; private set; } = default!;
    public string? AssigneeName { get; private set; }
    public required DateTime CreatedAt { get; init; }
    public int? GroupId { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public ReviewStatus ReviewStatus { get; private set; } = ReviewStatus.None;
    public int? ReviewedInSprintId { get; private set; }

    private readonly List<EvidenceLink> _evidenceLinks = [];
    public IReadOnlyList<EvidenceLink> EvidenceLinks => _evidenceLinks.AsReadOnly();
}
