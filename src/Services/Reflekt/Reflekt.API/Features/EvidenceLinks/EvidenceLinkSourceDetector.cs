using System.Text.RegularExpressions;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.EvidenceLinks;

public static partial class EvidenceLinkSourceDetector
{
    [GeneratedRegex(@"[A-Z][A-Z0-9]+-\d+", RegexOptions.CultureInvariant)]
    private static partial Regex JiraKeyRegex();

    public static (EvidenceLinkSource Source, string? ExternalKey) Detect(string url, string? jiraDomain)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (EvidenceLinkSource.Generic, null);

        var host = uri.Host.ToLowerInvariant();

        if (host == "github.com")
            return (EvidenceLinkSource.GitHub, null);

        if (!string.IsNullOrWhiteSpace(jiraDomain) &&
            host.Equals(jiraDomain.ToLowerInvariant(), StringComparison.Ordinal))
        {
            var key = ExtractJiraKey(uri.AbsolutePath);
            return (EvidenceLinkSource.Jira, key);
        }

        var jiraKey = ExtractJiraKeyFromBrowsePath(uri.AbsolutePath);
        if (jiraKey is not null)
            return (EvidenceLinkSource.Jira, jiraKey);

        return (EvidenceLinkSource.Generic, null);
    }

    private static string? ExtractJiraKey(string path)
    {
        var match = JiraKeyRegex().Match(path);
        return match.Success ? match.Value : null;
    }

    private static string? ExtractJiraKeyFromBrowsePath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals("browse", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = segments[i + 1];
                if (JiraKeyRegex().IsMatch(candidate))
                    return candidate;
            }
        }
        return null;
    }
}
