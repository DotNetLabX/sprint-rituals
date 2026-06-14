
namespace Reflekt.Domain.Participants.Events;

public sealed record ParticipantJoinedEvent(Participant Participant) : IDomainEvent;
