using Identity.Domain.Enums;
using Identity.Domain.Persons.Events;

namespace Identity.Domain.Persons;

public partial class Person
{
    public static Person CreateFromGoogle(string googleId, string firstName, string lastName, string email, string? pictureUrl)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PictureUrl = pictureUrl,
            Source = PersonSource.Google,
            ExternalId = googleId,
            CreatedAt = DateTime.UtcNow
        };

        // PersonCreated is NOT raised here — Id is still 0 before SaveChangesAsync.
        // Callers must call person.RaiseCreated() after persisting, when EF has assigned the real PK.
        return person;
    }

    /// <summary>
    /// Raises PersonCreated with the real persisted Id.
    /// Call AFTER SaveChangesAsync so EF has assigned the PK.
    /// </summary>
    public void RaiseCreated() => AddDomainEvent(new PersonCreated(Id));

    public void UpdateFromGoogle(string firstName, string lastName, string? pictureUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        PictureUrl = pictureUrl;
    }

    public void LinkUser(int userId)
    {
        UserId = userId;
    }
}
