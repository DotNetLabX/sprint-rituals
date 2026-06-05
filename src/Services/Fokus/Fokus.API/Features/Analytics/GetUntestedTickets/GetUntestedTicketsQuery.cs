namespace Fokus.API.Features.Analytics.GetUntestedTickets;

public class GetUntestedTicketsRequest
{
    public int SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetUntestedTicketsRequestValidator : Validator<GetUntestedTicketsRequest>
{
    public GetUntestedTicketsRequestValidator()
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

public class UntestedTicketsResponse
{
    public List<UntestedTicketItem> Tickets { get; set; } = [];
}

public class UntestedTicketItem
{
    public string TicketKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public decimal? StoryPoints { get; set; }
}
