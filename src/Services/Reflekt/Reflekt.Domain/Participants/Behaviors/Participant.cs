namespace Reflekt.Domain.Participants;

public sealed partial class Participant
{
    public static Participant Join(string displayName, int sprintId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var participant = new Participant
        {
            SprintId = sprintId,
            DisplayName = displayName,
            GuestToken = Guid.NewGuid().ToString(),
            JoinedAt = DateTime.UtcNow,
        };
        participant.AddDomainEvent(new Events.ParticipantJoinedEvent(participant));
        return participant;
    }

    public static Participant JoinWithGoogle(string displayName, int sprintId, int userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var participant = new Participant
        {
            SprintId = sprintId,
            DisplayName = displayName,
            GuestToken = Guid.NewGuid().ToString(),
            JoinedAt = DateTime.UtcNow,
            UserId = userId,
        };
        participant.AddDomainEvent(new Events.ParticipantJoinedEvent(participant));
        return participant;
    }

    public void Kick()
    {
        AddDomainEvent(new Events.ParticipantKickedEvent(this));
    }
}
