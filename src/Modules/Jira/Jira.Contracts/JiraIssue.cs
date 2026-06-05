using System.Text.Json.Serialization;

namespace Jira.Contracts;

public class JiraIssue
{
    public string Key { get; set; } = string.Empty;
    public JiraIssueFields Fields { get; set; } = new();
    public JiraChangelog Changelog { get; set; } = new();
}

public class JiraIssueFields
{
    public string Summary { get; set; } = string.Empty;
    public JiraIssueType? Issuetype { get; set; }
    [JsonPropertyName("customfield_10016")]
    public decimal? StoryPoints { get; set; }
    [JsonPropertyName("customfield_10008")]
    public string? EpicKey { get; set; }
    [JsonPropertyName("customfield_10014")]
    public string? EpicName { get; set; }
    public JiraUser? Assignee { get; set; }
    public JiraPriority? Priority { get; set; }
    public JiraStatus? Status { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Resolutiondate { get; set; }
    public JiraParent? Parent { get; set; }
    public List<JiraIssueLink>? Issuelinks { get; set; }
}

public class JiraIssueLink
{
    public JiraIssueLinkType? Type { get; set; }
    public JiraLinkedIssue? OutwardIssue { get; set; }
    public JiraLinkedIssue? InwardIssue { get; set; }
}

public class JiraIssueLinkType
{
    public string Name { get; set; } = string.Empty;
    public string Inward { get; set; } = string.Empty;
    public string Outward { get; set; } = string.Empty;
}

public class JiraLinkedIssue
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public JiraLinkedIssueFields Fields { get; set; } = new();
}

public class JiraLinkedIssueFields
{
    public string Summary { get; set; } = string.Empty;
    public JiraStatus? Status { get; set; }
    public JiraIssueType? Issuetype { get; set; }
    public JiraUser? Assignee { get; set; }
    public DateTime? Created { get; set; }
}

public class JiraParent
{
    public string Key { get; set; } = string.Empty;
    public JiraParentFields Fields { get; set; } = new();
}

public class JiraParentFields
{
    public string Summary { get; set; } = string.Empty;
    public JiraIssueType? Issuetype { get; set; }
}

public class JiraIssueType
{
    public string Name { get; set; } = string.Empty;
}

public class JiraUser
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> AvatarUrls { get; set; } = [];
}

public class JiraPriority
{
    public string Name { get; set; } = string.Empty;
}

public class JiraStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JiraStatusCategory? StatusCategory { get; set; }
    public string CategoryKey => StatusCategory?.Key ?? string.Empty;
}

public class JiraStatusCategory
{
    public string Key { get; set; } = string.Empty;
}
