namespace Fokus.Domain;

public partial class StatusTransition
{
    public static List<StatusTransition> ListFromJira(JiraIssue dto)
    {
        var transitions = new List<StatusTransition>();

        var histories = dto.Changelog?.Histories;
        if (histories is null || histories.Count == 0) return transitions;

        foreach (var history in histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field == "status"))
            {
                transitions.Add(new StatusTransition
                {
                    TicketId = dto.Key,
                    FromStatus = item.FromString ?? string.Empty,
                    ToStatus = item.ToStringValue ?? string.Empty,
                    Timestamp = history.Created,
                    AuthorId = history.Author.AccountId
                });
            }
        }

        return transitions;
    }
}
