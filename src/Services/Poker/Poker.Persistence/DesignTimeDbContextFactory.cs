using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Poker.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Persistence-as-startup: no Program.cs — factory provides the context for dotnet ef.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PokerDbContext>
{
    public PokerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PokerDbContext>();
        optionsBuilder.UseSqlite("Data Source=poker.db");
        return new PokerDbContext(optionsBuilder.Options);
    }
}
