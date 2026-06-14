namespace Reflekt.Domain.Participants;

public sealed partial class Participant
{
    /// <summary>
    /// Joins a retro. <paramref name="personId"/> is the Identity <c>Person</c> id for an
    /// authenticated suite member (from the cookie's <c>person_id</c> claim); null for a guest
    /// (ADR-022). One factory, one nullable parameter — this keeps join a single interceptable
    /// creation point so a future SM-approval gate (ADR-021) slots in before this call.
    /// </summary>
    public static Participant Join(string displayName, int sprintId, int? personId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var participant = new Participant
        {
            SprintId = sprintId,
            DisplayName = displayName,
            GuestToken = Guid.NewGuid().ToString(),
            JoinedAt = DateTime.UtcNow,
            PersonId = personId,
        };
        participant.AddDomainEvent(new Events.ParticipantJoinedEvent(participant));
        return participant;
    }

    public void Kick()
    {
        AddDomainEvent(new Events.ParticipantKickedEvent(this));
    }
}
