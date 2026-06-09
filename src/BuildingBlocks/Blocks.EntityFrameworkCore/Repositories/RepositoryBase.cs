using Blocks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Blocks.EntityFrameworkCore.Repositories;

public abstract class RepositoryBase<TContext, TEntity, TKey>(TContext dbContext)
    where TContext : DbContext
    where TEntity : class, IEntity<TKey>
{
    protected readonly TContext DbContext = dbContext;

    protected DbSet<TEntity> Entity => DbContext.Set<TEntity>();

    public virtual IQueryable<TEntity> Query() => Entity;

    public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken ct = default)
        => await Entity.FindAsync([id], ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Entity.AddAsync(entity, ct);

    public async Task UpsertAsync(TEntity entity, CancellationToken ct = default)
    {
        var existing = await FindByIdAsync(entity.Id, ct);
        if (existing is null)
            await Entity.AddAsync(entity, ct);
        else
            // Copy via EF PropertyValues so properties mapped to private backing fields
            // (PropertyAccessMode.Field) are included. Passing the detached POCO directly
            // to SetValues(object) uses plain reflection and misses field-backed properties.
            // Note: this base method has no production callers — all four Fokus repositories
            // hide it with `public new UpsertAsync` and do explicit per-column copy.
            // This hardens the base for future flat-entity repositories that do not override.
            DbContext.Entry(existing).CurrentValues.SetValues(DbContext.Entry(entity).CurrentValues);
    }

    public async Task DeleteByIdAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(id, ct);
        if (entity is not null)
            Entity.Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => DbContext.SaveChangesAsync(ct);
}
