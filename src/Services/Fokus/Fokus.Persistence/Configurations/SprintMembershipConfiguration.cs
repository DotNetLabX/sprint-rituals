namespace Fokus.Persistence.Configurations;

public class SprintMembershipConfiguration : IEntityTypeConfiguration<SprintMembership>
{
    public void Configure(EntityTypeBuilder<SprintMembership> builder)
    {
        builder.HasKey(sm => new { sm.SprintId, sm.TicketId });

        builder.Property(sm => sm.TicketId).HasMaxLength(64);
        builder.Property(sm => sm.FinalStatus).IsRequired().HasMaxLength(128);
        builder.Property(sm => sm.StoryPoints).HasColumnType("decimal(8,2)");

        builder.HasOne(sm => sm.Sprint)
            .WithMany(s => s.Memberships)
            .HasForeignKey(sm => sm.SprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sm => sm.Ticket)
            .WithMany()
            .HasForeignKey(sm => sm.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sm => sm.TicketId).HasDatabaseName("IX_SprintMembership_TicketId");
    }
}
