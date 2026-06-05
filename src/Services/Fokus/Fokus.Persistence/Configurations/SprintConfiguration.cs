using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class SprintConfiguration : AuditedEntityConfiguration<Sprint>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<Sprint> builder)
    {
        base.Configure(builder);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(256);
        builder.Property(s => s.BoardName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.State).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.Goal).HasMaxLength(1024);
    }
}
