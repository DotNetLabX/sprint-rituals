using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blocks.EntityFrameworkCore.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Runs EF Core migrations for <typeparamref name="TDbContext"/> at startup.
    /// For SQLite databases, ensures the parent directory exists before migrating
    /// so that first-run on a clean machine creates the data directory automatically.
    /// </summary>
    public static IHost Migrate<TDbContext>(this IHost host)
        where TDbContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        EnsureDataDirectory(context);
        context.Database.Migrate();

        return host;
    }

    private static void EnsureDataDirectory(DbContext context)
    {
        var connStr = context.Database.GetConnectionString();
        if (connStr is null) return;

        // Parse "Data Source=<path>" — works for SQLite connection strings.
        // Split on the first '=' only so paths containing '=' are handled correctly.
        var dataSource = ParseDataSource(connStr);
        if (string.IsNullOrWhiteSpace(dataSource)) return;

        var fullPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(Directory.GetCurrentDirectory(), dataSource);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static string? ParseDataSource(string connectionString)
    {
        foreach (var segment in connectionString.Split(';'))
        {
            var idx = segment.IndexOf('=');
            if (idx < 0) continue;

            var key = segment[..idx].Trim();
            if (string.Equals(key, "Data Source", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "DataSource", StringComparison.OrdinalIgnoreCase))
            {
                return segment[(idx + 1)..].Trim();
            }
        }

        return null;
    }
}
