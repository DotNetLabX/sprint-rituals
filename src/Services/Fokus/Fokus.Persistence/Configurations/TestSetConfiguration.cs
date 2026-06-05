using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class TestSetConfiguration : EntityConfiguration<TestSet, string>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<TestSet> builder)
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

        builder.HasIndex(t => t.AssigneeId).HasDatabaseName("IX_TestSet_AssigneeId");
    }
}
