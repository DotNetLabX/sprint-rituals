namespace Fokus.Domain;

public class TestExecutionLink
{
    public required string TestExecutionIssueId { get; set; }
    public required string TicketKey { get; set; }
    public TestExecutionLinkType LinkType { get; set; }

    public TestExecution TestExecution { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
