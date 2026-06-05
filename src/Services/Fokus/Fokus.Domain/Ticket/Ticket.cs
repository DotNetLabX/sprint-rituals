namespace Fokus.Domain;

public partial class Ticket : AggregateRoot<string>
{
    public required string Summary { get; set; }
    public required string IssueType { get; set; }
    public decimal? StoryPoints { get; set; }
    public string? EpicKey { get; set; }
    public string? EpicName { get; set; }
    public string? ParentTicketKey { get; set; }
    public string? AssigneeId { get; set; }
    public required string Priority { get; set; }
    public required string CurrentStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    public Developer? Assignee { get; set; }

    private readonly List<StatusTransition> _statusTransitions = [];
    public IReadOnlyList<StatusTransition> StatusTransitions => _statusTransitions.AsReadOnly();
}
