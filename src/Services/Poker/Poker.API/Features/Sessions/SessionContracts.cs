using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Participants;
using Poker.Domain.Planning.Votes;

namespace Poker.API.Features.Sessions;

// ---- Shared response DTOs ----

/// <summary>
/// Public participant shape returned in session listings.
/// GuestToken is intentionally omitted — it is a session takeover credential and must
/// never be exposed to arbitrary callers via the (anonymous) GET session endpoint.
/// </summary>
public record ParticipantResponse(
    Guid Id,
    string DisplayName,
    bool IsVoter,
    int? PersonId);

/// <summary>
/// One-time response returned to the joining participant only.
/// Contains the GuestToken (auth credential) — never included in the session list.
/// The sessionId field lets the client store the token under the correct key.
/// </summary>
public record JoinSessionResponse(
    Guid ParticipantId,
    string GuestToken,
    string DisplayName,
    Guid SessionId);

public record VoteResponse(
    Guid ParticipantId,
    string Value);

public record EstimationItemResponse(
    Guid Id,
    string Label,
    string? TicketKey,
    string Status,
    string? FinalEstimate,
    int Order,
    IReadOnlyList<VoteResponse> Votes);

public record SessionResponse(
    Guid Id,
    string Mode,
    string Status,
    int? SprintId,
    string? SprintName,
    string? Title,
    int CreatedByPersonId,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyList<ParticipantResponse> Participants,
    IReadOnlyList<EstimationItemResponse> Items);

public record SessionSummaryResponse(
    Guid Id,
    string Mode,
    string? SprintName,
    string? Title,
    int ItemsEstimated,
    int ItemsSkipped,
    decimal TotalStoryPoints,
    DateTime? ClosedAt,
    IReadOnlyList<EstimationItemResponse> Items);

public static class SessionMappings
{
    public static ParticipantResponse ToResponse(this SessionParticipant p) => new(
        p.Id, p.DisplayName, p.IsVoter, p.PersonId);

    public static JoinSessionResponse ToJoinResponse(this SessionParticipant p, Guid sessionId) => new(
        p.Id, p.GuestToken, p.DisplayName, sessionId);

    public static VoteResponse ToResponse(this Vote v) => new(v.ParticipantId, v.Value);

    public static EstimationItemResponse ToResponse(this EstimationItem i) => new(
        i.Id, i.Label, i.TicketKey, i.Status.ToString(), i.FinalEstimate, i.Order,
        i.Votes.Select(v => v.ToResponse()).ToList());

    public static SessionResponse ToResponse(this PlanningSession s) => new(
        s.Id, s.Mode.ToString(), s.Status.ToString(),
        s.SprintId, s.SprintName, s.Title, s.CreatedByPersonId,
        s.CreatedAt, s.ClosedAt,
        s.Participants.Select(p => p.ToResponse()).ToList(),
        s.Items.Select(i => i.ToResponse()).ToList());

    public static SessionSummaryResponse ToSummaryResponse(this PlanningSession s)
    {
        var estimated = s.Items.Count(i => i.Status == ItemStatus.Decided);
        var skipped = s.Items.Count(i => i.Status == ItemStatus.Skipped);
        var total = s.Items
            .Where(i => i.Status == ItemStatus.Decided && i.FinalEstimate is not null)
            .Sum(i => decimal.TryParse(i.FinalEstimate, out var v) ? v : 0m);

        return new SessionSummaryResponse(
            s.Id, s.Mode.ToString(), s.SprintName, s.Title,
            estimated, skipped, total, s.ClosedAt,
            s.Items.Select(i => i.ToResponse()).ToList());
    }
}
