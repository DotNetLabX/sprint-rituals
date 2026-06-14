using Microsoft.EntityFrameworkCore.Design;

namespace Reflekt.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Persistence-as-startup: no Program.cs — factory provides the context for dotnet ef.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReflektDbContext>
{
    public ReflektDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReflektDbContext>();
        optionsBuilder.UseSqlite("Data Source=reflekt.db");
        return new ReflektDbContext(optionsBuilder.Options);
    }
}
