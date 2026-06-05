using Blocks.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Domain.Persons;

public partial class Person : AggregateRoot<int>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public required string Email { get; set; }
    public string? PictureUrl { get; set; }
    public PersonSource Source { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
}
