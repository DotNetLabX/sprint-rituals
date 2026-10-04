namespace Reflekt.API.Features.RetroSessions.GetAvailableSprints;

public sealed record GetAvailableSprintsQuery(int TeamId);

public sealed record AvailableSprintResponse(int SprintId, string Name, DateTime StartDate, DateTime EndDate, string State);
