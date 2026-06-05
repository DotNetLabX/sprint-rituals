namespace Fokus.Domain;

public partial class SprintMembership
{
    public int SprintId { get; set; }
    public required string TicketId { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public bool WasCommitted { get; set; }
    public required string FinalStatus { get; set; }
    public decimal? StoryPoints { get; set; }

    public Sprint Sprint { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
