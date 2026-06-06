namespace Fokus.Domain;

public partial class Sprint
{
    public void AddMembership(SprintMembership membership) => _memberships.Add(membership);
}
