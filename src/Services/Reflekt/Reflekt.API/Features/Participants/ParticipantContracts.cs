namespace Reflekt.API.Features.Participants;

// Wire-stability carve-out (RA2 / plan Step 3): the JSON field stays `UserId` for the frontend
// (out of RA2 scope), but its source is now `Participant.PersonId` — mapped explicitly in
// ParticipantMappings. This is the one sanctioned exception to the "zero `.UserId`" grep rule.
public sealed record JoinRetroResponse(int Id, int RetroSessionId, string DisplayName, string GuestToken, DateTime JoinedAt, int? UserId);

public sealed record ParticipantResponse(int Id, int RetroSessionId, string DisplayName, DateTime JoinedAt);

