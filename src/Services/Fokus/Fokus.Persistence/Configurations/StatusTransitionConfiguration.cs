using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class StatusTransitionConfiguration : EntityConfiguration<StatusTransition>
{
    protected override bool HasGeneratedId => true;

    public override void Configure(EntityTypeBuilder<StatusTransition> builder)
    {
        base.Configure(builder);

        builder.Property(st => st.TicketId).IsRequired().HasMaxLength(64);
        builder.Property(st => st.FromStatus).IsRequired().HasMaxLength(128);
        builder.Property(st => st.ToStatus).IsRequired().HasMaxLength(128);
        builder.Property(st => st.AuthorId).HasMaxLength(128);

        builder.HasOne(st => st.Ticket)
            .WithMany(t => t.StatusTransitions)
            .HasForeignKey(st => st.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(st => new { st.TicketId, st.Timestamp })
            .HasDatabaseName("IX_StatusTransition_TicketId_Timestamp");
    }
}
