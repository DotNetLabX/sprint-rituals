
namespace Reflekt.Domain.Votes.Events;

public sealed record VoteCastEvent(Vote Vote) : IDomainEvent;
