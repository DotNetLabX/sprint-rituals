namespace Fokus.Domain;

public partial class StatusTransition : Entity<int>
{
    public required string TicketId { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AuthorId { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
