
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class ParticipantConfiguration : EntityConfiguration<Participant>
{
    public override void Configure(EntityTypeBuilder<Participant> builder)
    {
        base.Configure(builder);
        builder.ToTable("Participants");
        builder.Property(p => p.SprintId).IsRequired();
        builder.HasOne<RetroSession>().WithMany().HasForeignKey(p => p.SprintId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(p => p.GuestToken).IsRequired();
        builder.HasIndex(p => p.GuestToken).IsUnique();
        builder.HasIndex(p => p.SprintId);
        builder.Property(p => p.JoinedAt).IsRequired();
        // PersonId is an Identity Person id (cross-module, owned via cookie claim — no Reflekt-local FK). RA2/ADR-016.
        builder.Property(p => p.PersonId).IsRequired(false);
        builder.HasIndex(p => new { p.SprintId, p.PersonId }).IsUnique().HasFilter("\"PersonId\" IS NOT NULL");
        builder.HasIndex(p => p.PersonId);
    }
}

