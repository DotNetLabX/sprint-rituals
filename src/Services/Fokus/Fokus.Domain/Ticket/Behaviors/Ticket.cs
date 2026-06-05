namespace Fokus.Domain;

public partial class Ticket
{
    public static Ticket FromJira(JiraIssue dto) => new()
    {
        Id = dto.Key,
        Summary = dto.Fields.Summary,
        IssueType = dto.Fields.Issuetype?.Name ?? "Unknown",
        StoryPoints = dto.Fields.StoryPoints,
        EpicKey = dto.Fields.EpicKey ?? (dto.Fields.Parent?.Fields.Issuetype?.Name == "Epic" ? dto.Fields.Parent.Key : null),
        EpicName = dto.Fields.EpicName ?? (dto.Fields.Parent?.Fields.Issuetype?.Name == "Epic" ? dto.Fields.Parent.Fields.Summary : null),
        ParentTicketKey = dto.Fields.Parent?.Fields.Issuetype?.Name != "Epic" ? dto.Fields.Parent?.Key : null,
        AssigneeId = dto.Fields.Assignee?.AccountId,
        Priority = dto.Fields.Priority?.Name ?? "Medium",
        CurrentStatus = dto.Fields.Status?.Name ?? "Unknown",
        CreatedDate = dto.Fields.Created ?? DateTime.UtcNow,
        ResolvedDate = dto.Fields.Resolutiondate
    };
}
