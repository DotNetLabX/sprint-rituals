using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Users;

namespace Reflekt.Persistence.Repositories;

public sealed class UserRepository(ReflektDbContext db) : Repository<User>(db)
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => Db.Users
            .FirstOrDefaultAsync(u => EF.Functions.Collate(u.Email, "NOCASE") == email, ct);
}
