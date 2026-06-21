using Blocks.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Votes;

namespace Poker.Persistence.EntityConfigurations;

internal sealed class VoteConfiguration : EntityConfiguration<Vote, Guid>
{
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<Vote> builder)
    {
        base.Configure(builder);
        builder.ToTable("Votes");

        builder.Property(v => v.ItemId).IsRequired();

        // ParticipantId: cross-aggregate reference by id — no FK (participant lives in same module
        // but owned via EstimationItem cascade makes direct FK redundant; enforced by Rule 21 index)
        builder.Property(v => v.ParticipantId).IsRequired();

        // Rule 21: one vote per participant per item
        builder.HasIndex(v => new { v.ItemId, v.ParticipantId }).IsUnique();

        // Value: Fibonacci string including "?"
        builder.Property(v => v.Value).IsRequired().HasMaxLength(5);
        builder.Property(v => v.SubmittedAt).IsRequired();
    }
}
