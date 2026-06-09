using Blocks.Domain.Entities;

namespace Identity.Domain.Users;

public partial class User : AggregateRoot<int>
{
    public int PersonId { get; private set; }
    public string Role { get; private set; } = "Manager";
    public bool IsActive { get; private set; } = true;
    public DateTime RegistrationDate { get; private set; }
    public DateTime? LastLogin { get; private set; }

    private User() { }
}
