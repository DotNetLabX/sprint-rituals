using Blocks.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Poker.Domain.Planning.Participants;

namespace Poker.Persistence.EntityConfigurations;

internal sealed class SessionParticipantConfiguration : EntityConfiguration<SessionParticipant, Guid>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<SessionParticipant> builder)
    {
        base.Configure(builder);
        builder.ToTable("SessionParticipants");

        builder.Property(p => p.SessionId).IsRequired();

        // PersonId: Identity Person id — cross-module, no FK. Null for guests (ADR-022).
        builder.Property(p => p.PersonId);

        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);

        // GuestToken: session-scoped, used by PokerGuestTokenAuthenticationHandler
        builder.Property(p => p.GuestToken).IsRequired();
        builder.HasIndex(p => p.GuestToken).IsUnique();

        builder.HasIndex(p => p.SessionId);

        // Unique: one authenticated participant (PersonId) per session (Rule 15 idempotency)
        builder.HasIndex(p => new { p.SessionId, p.PersonId })
            .IsUnique()
            .HasFilter("\"PersonId\" IS NOT NULL");

        builder.Property(p => p.JoinedAt).IsRequired();
    }
}
