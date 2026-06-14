
namespace Reflekt.Domain.Votes.Events;

public sealed record VoteRetractedEvent(Vote Vote) : IDomainEvent;
