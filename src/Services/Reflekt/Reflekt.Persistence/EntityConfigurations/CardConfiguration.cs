
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class CardConfiguration : EntityConfiguration<Card>
{
    public override void Configure(EntityTypeBuilder<Card> builder)
    {
        base.Configure(builder);
        builder.ToTable("Cards");
        builder.Property(c => c.SprintId).IsRequired();
        builder.HasOne<Sprint>().WithMany().HasForeignKey(c => c.SprintId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(c => c.ParticipantId).IsRequired();
        builder.HasOne<Participant>().WithMany().HasForeignKey(c => c.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.ColumnType).IsRequired();
        builder.Property(c => c.CardType).IsRequired();
        builder.Property(c => c.Text).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.AssigneeName).HasMaxLength(50);
        builder.HasOne<CardGroup>().WithMany().HasForeignKey(c => c.GroupId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(c => c.SprintId);
        builder.HasIndex(c => c.GroupId);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.Property(c => c.ReviewStatus).IsRequired().HasDefaultValue(ReviewStatus.None);
        builder.HasOne<Sprint>().WithMany().HasForeignKey(c => c.ReviewedInSprintId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(c => c.ReviewedInSprintId);

        builder.HasMany(c => c.EvidenceLinks)
            .WithOne()
            .HasForeignKey(e => e.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.EvidenceLinks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
