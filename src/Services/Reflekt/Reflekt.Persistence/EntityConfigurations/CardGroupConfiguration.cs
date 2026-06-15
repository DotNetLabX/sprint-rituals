
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class CardGroupConfiguration : EntityConfiguration<CardGroup>
{
    public override void Configure(EntityTypeBuilder<CardGroup> builder)
    {
        base.Configure(builder);
        builder.ToTable("CardGroups");
        builder.Property(g => g.SprintId).IsRequired();
        // SprintId FK column name preserved per Q1=b DO-NOT-TOUCH
        builder.HasOne<RetroSession>().WithMany().HasForeignKey(g => g.SprintId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(g => g.ColumnType).IsRequired();
        builder.Property(g => g.Title).HasMaxLength(100);
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.DiscussedAt);
        builder.HasIndex(g => g.SprintId);
    }
}
