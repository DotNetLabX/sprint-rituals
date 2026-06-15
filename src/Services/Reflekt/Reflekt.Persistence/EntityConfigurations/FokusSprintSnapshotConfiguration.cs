using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.FokusSprints;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class FokusSprintSnapshotConfiguration : IEntityTypeConfiguration<FokusSprintSnapshot>
{
    public void Configure(EntityTypeBuilder<FokusSprintSnapshot> builder)
    {
        builder.ToTable("FokusSprintSnapshots");
        // SprintId is the natural Fokus sprint ID — ValueGeneratedNever (we control it, not the DB)
        builder.HasKey(s => s.SprintId);
        builder.Property(s => s.SprintId).ValueGeneratedNever();
        builder.Property(s => s.SprintName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BoardId).IsRequired();
        builder.Property(s => s.SyncedAt).IsRequired();
        builder.Property(s => s.State).IsRequired().HasMaxLength(50);
    }
}
