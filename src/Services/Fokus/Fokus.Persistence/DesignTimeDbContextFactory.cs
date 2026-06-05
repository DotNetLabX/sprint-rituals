using Microsoft.EntityFrameworkCore.Design;

namespace Fokus.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FokusDbContext>
{
    public FokusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FokusDbContext>();
        optionsBuilder.UseSqlite("Data Source=fokus.db");
        return new FokusDbContext(optionsBuilder.Options);
    }
}
