using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class TestExecutionConfiguration : EntityConfiguration<TestExecution, string>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<TestExecution> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.Id).HasMaxLength(128);
        builder.Property(t => t.IssueKey).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Summary).IsRequired().HasMaxLength(1024);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(128);
        builder.Property(t => t.AssigneeId).HasMaxLength(128);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Links)
            .WithOne(l => l.TestExecution)
            .HasForeignKey(l => l.TestExecutionIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TestRuns)
            .WithOne(r => r.TestExecution)
            .HasForeignKey(r => r.TestExecutionIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.AssigneeId).HasDatabaseName("IX_TestExecution_AssigneeId");
    }
}
