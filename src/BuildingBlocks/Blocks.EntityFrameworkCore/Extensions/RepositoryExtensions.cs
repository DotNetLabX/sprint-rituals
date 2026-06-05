using Blocks.Domain.Entities;
using Blocks.EntityFrameworkCore.Repositories;
using Blocks.Exceptions;

namespace Blocks.EntityFrameworkCore.Extensions;

public static class RepositoryExtensions
{
    public static async Task<TEntity> FindByIdOrThrowAsync<TContext, TEntity, TKey>(
        this RepositoryBase<TContext, TEntity, TKey> repository,
        TKey id,
        CancellationToken ct = default)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
        where TEntity : class, IEntity<TKey>
    {
        var entity = await repository.FindByIdAsync(id, ct);
        return entity ?? throw new NotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");
    }
}
