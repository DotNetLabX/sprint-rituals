using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class TestRunConfiguration : EntityConfiguration<TestRun, string>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<TestRun> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.Id).HasMaxLength(256);
        builder.Property(r => r.TestExecutionIssueId).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TestRunStatus>(v))
            .IsRequired()
            .HasMaxLength(32);
        builder.Property(r => r.StatusName).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ExecutedById).HasMaxLength(128);

        builder.HasOne(r => r.TestExecution)
            .WithMany(t => t.TestRuns)
            .HasForeignKey(r => r.TestExecutionIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ExecutedBy)
            .WithMany()
            .HasForeignKey(r => r.ExecutedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.TestExecutionIssueId).HasDatabaseName("IX_TestRun_TestExecutionIssueId");
        builder.HasIndex(r => r.ExecutedById).HasDatabaseName("IX_TestRun_ExecutedById");
    }
}
