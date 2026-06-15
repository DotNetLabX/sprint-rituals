using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class FokusSprintRefConfiguration : IEntityTypeConfiguration<FokusSprintRef>
{
    public void Configure(EntityTypeBuilder<FokusSprintRef> builder)
    {
        builder.ToTable("FokusSprintRefs");
        builder.HasKey(r => new { r.RetroSessionId, r.FokusSprintId });
        builder.Property(r => r.RetroSessionId).IsRequired();
        builder.Property(r => r.FokusSprintId).IsRequired();
        builder.HasIndex(r => r.FokusSprintId);
    }
}
