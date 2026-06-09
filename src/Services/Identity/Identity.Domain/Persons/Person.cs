using Blocks.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Domain.Persons;

public partial class Person : AggregateRoot<int>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    /// <summary>
    /// Email address. Nullable — Jira-sourced Persons may have no email
    /// (Jira Cloud hides developer email by default). Google-sourced Persons always have email.
    /// </summary>
    public string? Email { get; private set; }
    public string? PictureUrl { get; private set; }
    public PersonSource Source { get; private set; }
    public string? ExternalId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? UserId { get; private set; }

    // Parameterless ctor for EF Core materialization.
    private Person() { }
}
