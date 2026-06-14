namespace Reflekt.API.Features.Sprints;

public sealed record SprintResponse(
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

public sealed record SprintSummaryParticipant(string DisplayName);

public sealed record SprintSummaryResponse(
    int Id,
    int TeamId,
    string Name,
    string State,
    string CurrentPhase,
    int VotesPerParticipant,
    bool IsScrumMaster,
    List<SprintSummaryParticipant> Participants,
    string Column1Name,
    string Column1Color,
    string Column2Name,
    string Column2Color,
    string Column3Name,
    string Column3Color,
    int WritingTimerDefault,
    int VotingTimerDefault,
    int DiscussingTimerDefault);

public sealed record SprintRouteRequest(int TeamId, int SprintId);

