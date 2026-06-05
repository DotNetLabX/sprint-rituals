using Blocks.EntityFrameworkCore;

namespace Identity.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ApplicationDbContext<IdentityDbContext>(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
