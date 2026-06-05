using Blocks.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blocks.EntityFrameworkCore.EntityConfigurations;

public abstract class AuditedEntityConfiguration<TEntity> : EntityConfiguration<TEntity>
    where TEntity : AggregateRoot
{
    protected virtual bool HasConcurrencyToken => false;

    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.CreatedById).HasMaxLength(256);
        builder.Property(e => e.CreatedOn);
        builder.Property(e => e.LastModifiedById).HasMaxLength(256);
        builder.Property(e => e.LastModifiedOn);

        if (HasConcurrencyToken)
            builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

public abstract class AuditedEntityConfiguration<TEntity, TKey> : EntityConfiguration<TEntity, TKey>
    where TEntity : AggregateRoot<TKey>
{
    protected virtual bool HasConcurrencyToken => false;

    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.CreatedById).HasMaxLength(256);
        builder.Property(e => e.CreatedOn);
        builder.Property(e => e.LastModifiedById).HasMaxLength(256);
        builder.Property(e => e.LastModifiedOn);

        if (HasConcurrencyToken)
            builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
