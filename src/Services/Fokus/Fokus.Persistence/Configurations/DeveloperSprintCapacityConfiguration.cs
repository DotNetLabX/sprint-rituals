namespace Fokus.Persistence.Configurations;

public class DeveloperSprintCapacityConfiguration : IEntityTypeConfiguration<DeveloperSprintCapacity>
{
    public void Configure(EntityTypeBuilder<DeveloperSprintCapacity> builder)
    {
        builder.HasKey(c => new { c.DeveloperAccountId, c.SprintId });

        builder.Property(c => c.DeveloperAccountId).HasMaxLength(128);
        builder.Property(c => c.CapacityPercent).IsRequired();

        builder.HasOne(c => c.Developer)
            .WithMany()
            .HasForeignKey(c => c.DeveloperAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Sprint)
            .WithMany()
            .HasForeignKey(c => c.SprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.SprintId).HasDatabaseName("IX_DeveloperSprintCapacity_SprintId");
    }
}
