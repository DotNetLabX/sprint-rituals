namespace Fokus.Persistence.Configurations;

public class TestExecutionLinkConfiguration : IEntityTypeConfiguration<TestExecutionLink>
{
    public void Configure(EntityTypeBuilder<TestExecutionLink> builder)
    {
        builder.HasKey(l => new { l.TestExecutionIssueId, l.TicketKey });

        builder.Property(l => l.TestExecutionIssueId).HasMaxLength(128);
        builder.Property(l => l.TicketKey).HasMaxLength(64);
        builder.Property(l => l.LinkType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TestExecutionLinkType>(v))
            .IsRequired()
            .HasMaxLength(32);

        builder.HasOne(l => l.TestExecution)
            .WithMany(t => t.Links)
            .HasForeignKey(l => l.TestExecutionIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Ticket)
            .WithMany()
            .HasForeignKey(l => l.TicketKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.TicketKey).HasDatabaseName("IX_TestExecutionLink_TicketKey");
    }
}
