namespace Fokus.API.Features.Analytics.GetFailingTickets;

public class GetFailingTicketsRequest
{
    public int SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetFailingTicketsRequestValidator : Validator<GetFailingTicketsRequest>
{
    public GetFailingTicketsRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public class FailingTicketsResponse
{
    public List<FailingTicketItem> Tickets { get; set; } = [];
}

public class FailingTicketItem
{
    public string TicketKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public decimal? StoryPoints { get; set; }
    public int FailedRunCount { get; set; }
    public int TotalRunCount { get; set; }
}
