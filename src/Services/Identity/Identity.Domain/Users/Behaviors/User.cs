namespace Identity.Domain.Users;

public partial class User
{
    public static User CreateFromPerson(int personId, string role = "Manager")
    {
        return new User
        {
            PersonId = personId,
            Role = role,
            IsActive = true,
            RegistrationDate = DateTime.UtcNow
        };
    }

    public void ChangeRole(string role)
    {
        Role = role;
    }

    public void RecordLogin()
    {
        LastLogin = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
