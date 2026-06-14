
using Reflekt.Domain.Teams;
using Reflekt.Domain.Users;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class TeamConfiguration : EntityConfiguration<Team>
{
    public override void Configure(EntityTypeBuilder<Team> builder)
    {
        base.Configure(builder);
        builder.ToTable("Teams");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.OwnerId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique().HasDatabaseName("IX_Teams_OwnerId_Name");
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
