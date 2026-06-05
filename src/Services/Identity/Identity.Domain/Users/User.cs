using Blocks.Domain.Entities;

namespace Identity.Domain.Users;

public partial class User : Entity<int>
{
    public int PersonId { get; set; }
    public string Role { get; private set; } = "Manager";
    public bool IsActive { get; set; } = true;
    public DateTime RegistrationDate { get; set; }
    public DateTime? LastLogin { get; set; }
}
