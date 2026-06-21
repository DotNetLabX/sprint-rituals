using System.Text.Json.Serialization;

namespace Jira.Contracts;

/// <summary>
/// Partial update for a Jira issue's fields via PUT /rest/api/3/issue/{issueIdOrKey}.
/// Jira's issue-edit endpoint returns 204 No Content on success.
/// Only non-null fields are serialized (JsonIgnoreCondition.WhenWritingNull).
/// </summary>
public class UpdateIssueFieldsCommand
{
    [JsonPropertyName("fields")]
    public required IssueFields Fields { get; init; }

    public class IssueFields
    {
        /// <summary>
        /// Story points field using the resolved Jira custom field id.
        /// customfield_10016 is the id Fokus already reads via JiraIssueFields.StoryPoints
        /// (spec-confirmed, reuse the same id — v1 pins this, per-project variance is out of scope).
        /// </summary>
        [JsonPropertyName("customfield_10016")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? StoryPoints { get; init; }
    }
}
