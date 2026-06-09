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
    /// Creates a Jira-sourced Person. Email is best-effort (may be null — Jira Cloud hides email by default).
    /// Match/create key is ExternalId (Jira AccountId) + Source=Jira.
    /// </summary>
    public static Person CreateFromJira(string accountId, string firstName, string lastName, string? email, string? pictureUrl)
    {
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email is not null ? email.ToLowerInvariant() : null,
            PictureUrl = pictureUrl,
            Source = PersonSource.Jira,
            ExternalId = accountId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates a Jira-sourced Person from a sync. Email is updated only if a value is now known
    /// (email is best-effort from Jira Cloud; once set, it is not cleared).
    /// </summary>
    public void UpdateFromJira(string firstName, string lastName, string? email, string? pictureUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        PictureUrl = pictureUrl;
        if (email is not null && Email is null)
            Email = email.ToLowerInvariant();
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
