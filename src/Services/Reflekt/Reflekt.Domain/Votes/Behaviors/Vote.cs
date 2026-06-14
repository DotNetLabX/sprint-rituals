using Reflekt.Domain.Votes.Events;

namespace Reflekt.Domain.Votes;

public sealed partial class Vote
{
    public static Vote Create(int groupId, int participantId)
    {
        var vote = new Vote
        {
            GroupId = groupId,
            ParticipantId = participantId,
            CreatedAt = DateTime.UtcNow,
        };
        vote.AddDomainEvent(new VoteCastEvent(vote));
        return vote;
    }

    public void MarkRetracted()
    {
        AddDomainEvent(new VoteRetractedEvent(this));
    }
}
