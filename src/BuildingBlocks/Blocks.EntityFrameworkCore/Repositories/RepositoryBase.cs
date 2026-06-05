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
            DbContext.Entry(existing).CurrentValues.SetValues(entity);
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
