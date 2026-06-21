using Blocks.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Votes;

namespace Poker.Persistence.EntityConfigurations;

internal sealed class EstimationItemConfiguration : EntityConfiguration<EstimationItem, Guid>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<EstimationItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("EstimationItems");

        builder.Property(i => i.SessionId).IsRequired();

        builder.Property(i => i.Label).IsRequired().HasMaxLength(500);

        // TicketKey: Jira key — Sprint-bound only; absent in Free mode (Rule 19)
        builder.Property(i => i.TicketKey).HasMaxLength(50);

        builder.Property(i => i.Status).IsRequired()
            .HasConversion<string>();

        // FinalEstimate: set on Decide; null until then
        builder.Property(i => i.FinalEstimate).HasMaxLength(10);

        builder.Property(i => i.Order).IsRequired();
        builder.Property(i => i.DecidedAt);

        builder.HasIndex(i => i.SessionId);

        // Rule 19: exactly one item per ticket per session (Sprint-bound)
        builder.HasIndex(i => new { i.SessionId, i.TicketKey })
            .IsUnique()
            .HasFilter("\"TicketKey\" IS NOT NULL");

        // Children: Votes owned by item.
        // UsePropertyAccessMode(Field) so EF writes into the private _votes backing field.
        builder.HasMany(i => i.Votes)
            .WithOne()
            .HasForeignKey(v => v.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Votes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
