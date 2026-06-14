
namespace Reflekt.Domain.Users;

public sealed class User : AggregateRoot
{
    private User() { }

    public required string Email { get; init; }
    public string Name { get; private set; } = default!;
    public string GivenName { get; private set; } = default!;
    public required DateTime CreatedAt { get; init; }

    public static User Create(string email, string name, string givenName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(givenName);

        return new User
        {
            Email = email,
            Name = name,
            GivenName = givenName,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateProfile(string name, string givenName)
    {
        if (!string.Equals(Name, name, StringComparison.Ordinal))
            Name = name;

        if (!string.Equals(GivenName, givenName, StringComparison.Ordinal))
            GivenName = givenName;
    }
}
