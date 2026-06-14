
namespace Reflekt.Domain.Participants.Events;

public sealed record ParticipantKickedEvent(Participant Participant) : IDomainEvent;
