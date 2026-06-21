namespace Poker.Domain.Planning;

public enum SessionMode
{
    /// <summary>Items pulled from a Fokus sprint; agreed estimates written back to Jira.</summary>
    SprintBound,

    /// <summary>Items added manually by the SM; no Jira write-back.</summary>
    Free,
}
