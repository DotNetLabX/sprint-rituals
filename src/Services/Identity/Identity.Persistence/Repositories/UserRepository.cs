namespace Identity.Persistence.Repositories;

public class UserRepository(IdentityDbContext db)
{
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByPersonIdAsync(int personId, CancellationToken ct = default) =>
        await db.Users.SingleOrDefaultAsync(u => u.PersonId == personId, ct);

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await db.Users.CountAsync(ct);

    public async Task<int> CountActiveAdminsAsync(CancellationToken ct = default) =>
        await db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive, ct);

    /// <summary>
    /// Returns all users with their Person eagerly loaded, ordered by FirstName then LastName.
    /// Feeds the GetUsers list endpoint — all display fields (Email, name, AvatarUrl) live on Person.
    /// Manual join used because Person.FullName is a computed property ignored by EF;
    /// Include would still require a Select projection.
    /// </summary>
    public async Task<IReadOnlyList<(User User, Person Person)>> GetAllWithPersonAsync(CancellationToken ct = default)
    {
        var results = await db.Users
            .AsNoTracking()
            .Join(db.Persons,
                u => u.PersonId,
                p => p.Id,
                (u, p) => new { User = u, Person = p })
            .OrderBy(x => x.Person.FirstName)
            .ThenBy(x => x.Person.LastName)
            .ToListAsync(ct);

        return results.Select(x => (x.User, x.Person)).ToList().AsReadOnly();
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
