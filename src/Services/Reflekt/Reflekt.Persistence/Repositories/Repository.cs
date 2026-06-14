namespace Reflekt.Persistence.Repositories;

/// <summary>
/// Thin service-level repository binding to ReflektDbContext for int-keyed entities.
/// Adds Db alias and GetByIdAsync to match source API surface consumed by Reflekt's concrete repos.
/// </summary>
public class Repository<TEntity>(ReflektDbContext db)
    : RepositoryBase<ReflektDbContext, TEntity, int>(db)
    where TEntity : class, IEntity<int>
{
    /// <summary>Alias matching the source RepositoryBase's <c>Db</c> property name.</summary>
    protected ReflektDbContext Db => DbContext;

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await FindByIdAsync(id, ct);
}
