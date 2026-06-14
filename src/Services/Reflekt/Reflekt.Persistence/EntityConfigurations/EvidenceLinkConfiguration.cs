
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class EvidenceLinkConfiguration : EntityConfiguration<EvidenceLink>
{
    public override void Configure(EntityTypeBuilder<EvidenceLink> builder)
    {
        base.Configure(builder);
        builder.ToTable("EvidenceLinks");
        builder.Property(e => e.CardId).IsRequired();
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ExternalKey).HasMaxLength(50);
        builder.Property(e => e.FetchStatus).IsRequired();
        builder.HasIndex(e => e.CardId);
    }
}
