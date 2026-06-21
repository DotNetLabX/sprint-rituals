namespace Poker.Domain.Planning.Participants;

/// <summary>
/// A participant in a planning session — either a Google-authenticated suite member (PersonId set)
/// or a guest (PersonId null, ADR-022). Guests are session-scoped with no cross-session matching (Rule 15).
/// </summary>
public sealed class SessionParticipant : Entity<Guid>
{
    private SessionParticipant() { }

    public required Guid SessionId { get; init; }

    /// <summary>Identity Person id for authenticated participants; null for guests (ADR-022).</summary>
    public int? PersonId { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Session-scoped guest token used by PokerGuestTokenAuthenticationHandler.</summary>
    public required string GuestToken { get; init; }

    /// <summary>All participants are voters in v1 (Rule 4). Observer role is v2.</summary>
    public bool IsVoter => true;

    public required DateTime JoinedAt { get; init; }

    /// <summary>
    /// Factory: join the session as an authenticated Identity member (PersonId set).
    /// No guest token is generated — cookie-authed participants authenticate via the Identity
    /// cookie, not via X-Guest-Token. An empty token is stored to satisfy the NOT NULL column
    /// constraint; it is never returned to callers or used for authentication (defense-in-depth
    /// with the GuestToken omission from ParticipantResponse).
    /// </summary>
    public static SessionParticipant JoinAuthenticated(
        Guid sessionId,
        int personId,
        string displayName,
        DateTime joinedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PersonId = personId,
            DisplayName = displayName,
            GuestToken = string.Empty,  // Not used — cookie auth only; never returned to callers
            JoinedAt = joinedAt,
        };
    }

    /// <summary>
    /// Factory: join the session as a guest (PersonId null, ADR-022).
    /// </summary>
    public static SessionParticipant JoinAsGuest(
        Guid sessionId,
        string displayName,
        DateTime joinedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PersonId = null,
            DisplayName = displayName,
            GuestToken = Guid.NewGuid().ToString("N"),
            JoinedAt = joinedAt,
        };
    }
}
