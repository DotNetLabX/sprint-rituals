using Blocks.Domain.Entities;
using Blocks.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blocks.EntityFrameworkCore.EntityConfigurations;

public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity<int>
{
    protected virtual bool HasGeneratedId => true;

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        if (HasGeneratedId)
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
        else
            builder.Property(e => e.Id).ValueGeneratedNever();

        builder.SeedFromJsonFile();
    }
}

public abstract class EntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity<TKey>
{
    protected virtual bool HasGeneratedId => true;

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        if (HasGeneratedId)
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
        else
            builder.Property(e => e.Id).ValueGeneratedNever();

        builder.SeedFromJsonFile();
    }
}
