namespace Fokus.API.Features.Sync.GetJiraSprints;

public class GetJiraSprintsQuery
{
    public int BoardId { get; set; }
}

public class GetJiraSprintsResponse
{
    public List<JiraSprintDto> Sprints { get; set; } = [];
}

public class JiraSprintDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public string State { get; set; } = string.Empty;
}

public class GetJiraSprintsQueryValidator : Validator<GetJiraSprintsQuery>
{
    public GetJiraSprintsQueryValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .WithMessage("Board ID must be greater than 0.");
    }
}
