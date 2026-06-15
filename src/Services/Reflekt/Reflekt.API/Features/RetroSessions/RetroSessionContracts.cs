namespace Reflekt.API.Features.RetroSessions;

public sealed record RetroSessionResponse(
    int Id,
    int TeamId,
    string Name,
    string State,
    string CurrentPhase,
    int VotesPerParticipant,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    string Column1Name,
    string Column1Color,
    string Column2Name,
    string Column2Color,
    string Column3Name,
    string Column3Color,
    int WritingTimerDefault,
    int VotingTimerDefault,
    int DiscussingTimerDefault);

public sealed record RetroSessionSummaryParticipant(string DisplayName);

public sealed record RetroSessionSummaryResponse(
    int Id,
    int TeamId,
    string Name,
    string State,
    string CurrentPhase,
    int VotesPerParticipant,
    bool IsScrumMaster,
    List<RetroSessionSummaryParticipant> Participants,
    string Column1Name,
    string Column1Color,
    string Column2Name,
    string Column2Color,
    string Column3Name,
    string Column3Color,
    int WritingTimerDefault,
    int VotingTimerDefault,
    int DiscussingTimerDefault);

public sealed record RetroSessionRouteRequest(int TeamId, int RetroSessionId);
