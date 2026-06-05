using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class TicketConfiguration : AuditedEntityConfiguration<Ticket, string>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<Ticket> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.Id).HasMaxLength(64);

        builder.Property(t => t.Summary).IsRequired().HasMaxLength(1024);
        builder.Property(t => t.IssueType).IsRequired().HasMaxLength(64);
        builder.Property(t => t.EpicKey).HasMaxLength(64);
        builder.Property(t => t.EpicName).HasMaxLength(256);
        builder.Property(t => t.ParentTicketKey).HasMaxLength(64);
        builder.Property(t => t.AssigneeId).HasMaxLength(128);
        builder.Property(t => t.Priority).IsRequired().HasMaxLength(64);
        builder.Property(t => t.CurrentStatus).IsRequired().HasMaxLength(128);
        builder.Property(t => t.StoryPoints).HasColumnType("decimal(8,2)");

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.EpicKey).HasDatabaseName("IX_Ticket_EpicKey");
        builder.HasIndex(t => t.AssigneeId).HasDatabaseName("IX_Ticket_AssigneeId");
    }
}
