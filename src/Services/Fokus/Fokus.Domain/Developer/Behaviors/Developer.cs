namespace Fokus.Domain;

public partial class Developer
{
    public static Developer? FromJira(JiraIssue dto)
    {
        if (dto.Fields.Assignee is null) return null;

        var assignee = dto.Fields.Assignee;
        var avatarUrl = assignee.AvatarUrls.TryGetValue("48x48", out var url) ? url : null;

        return new Developer
        {
            Id = assignee.AccountId,
            DisplayName = assignee.DisplayName,
            AvatarUrl = avatarUrl,
            IsActive = true
        };
    }
}
