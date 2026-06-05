using System.ComponentModel.DataAnnotations;

namespace Jira.RestApi;

public class JiraOptions
{
    [Required]
    public string InstanceUrl { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ApiToken { get; set; } = string.Empty;
    public bool IsTeamManaged { get; set; } = true;
    public string? ProjectKey { get; set; }
}
