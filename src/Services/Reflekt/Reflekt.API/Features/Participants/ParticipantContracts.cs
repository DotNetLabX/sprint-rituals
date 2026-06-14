namespace Reflekt.API.Features.Participants;

public sealed record JoinSprintResponse(int Id, int SprintId, string DisplayName, string GuestToken, DateTime JoinedAt, int? UserId);

public sealed record ParticipantResponse(int Id, int SprintId, string DisplayName, DateTime JoinedAt);

