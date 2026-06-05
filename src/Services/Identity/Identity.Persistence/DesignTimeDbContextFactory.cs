using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlite("Data Source=identity.db");
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
