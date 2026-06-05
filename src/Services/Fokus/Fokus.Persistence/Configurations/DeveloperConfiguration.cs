using Blocks.EntityFrameworkCore.EntityConfigurations;

namespace Fokus.Persistence.Configurations;

public class DeveloperConfiguration : EntityConfiguration<Developer, string>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<Developer> builder)
    {
        base.Configure(builder);

        builder.Property(d => d.Id).HasMaxLength(128);
        builder.Property(d => d.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(d => d.AvatarUrl).HasMaxLength(1024);
        builder.Property(d => d.SubTeam).HasMaxLength(128);
        builder.Property(d => d.Role).IsRequired().HasMaxLength(64).HasDefaultValue("Developer");
        builder.Property(d => d.DefaultCapacityPercent).HasDefaultValue(100);
    }
}
