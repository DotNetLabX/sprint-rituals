
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class TeamConfiguration : EntityConfiguration<Team>
{
    public override void Configure(EntityTypeBuilder<Team> builder)
    {
        base.Configure(builder);
        builder.ToTable("Teams");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        // OwnerId is an Identity Person id (cross-module, owned via gRPC/cookie claim — no Reflekt-local FK). RA2/ADR-016.
        builder.Property(t => t.OwnerId).IsRequired();
        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique().HasDatabaseName("IX_Teams_OwnerId_Name");
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
