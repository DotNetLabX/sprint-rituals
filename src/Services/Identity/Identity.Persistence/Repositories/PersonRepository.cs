namespace Identity.Persistence.Repositories;

public class PersonRepository(IdentityDbContext db)
{
    public async Task<Person?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Persons.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Person?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Persons.SingleOrDefaultAsync(p => p.Email == email.ToLowerInvariant(), ct);

    public async Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        await db.Persons.SingleOrDefaultAsync(p => p.ExternalId == externalId, ct);

    public async Task<Person?> GetByExternalIdAndSourceAsync(string externalId, string source, CancellationToken ct = default) =>
        await db.Persons.SingleOrDefaultAsync(p => p.ExternalId == externalId && p.Source.ToString() == source, ct);

    public async Task AddAsync(Person person, CancellationToken ct = default) =>
        await db.Persons.AddAsync(person, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
