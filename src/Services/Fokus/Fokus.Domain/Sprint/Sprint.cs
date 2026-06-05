namespace Fokus.Domain;

public partial class Sprint : AggregateRoot
{
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int BoardId { get; set; }
    public required string BoardName { get; set; }
    public SprintState State { get; set; }
    public DateTime SyncedAt { get; set; }
    public string? Goal { get; set; }

    private readonly List<SprintMembership> _memberships = [];
    public IReadOnlyList<SprintMembership> Memberships => _memberships.AsReadOnly();
}
