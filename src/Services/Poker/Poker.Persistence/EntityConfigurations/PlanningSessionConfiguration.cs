using Blocks.EntityFrameworkCore.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Poker.Domain.Planning;
using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Participants;

namespace Poker.Persistence.EntityConfigurations;

internal sealed class PlanningSessionConfiguration : EntityConfiguration<PlanningSession, Guid>
{
    // Guid PKs are client-generated — never server-generated
    protected override bool HasGeneratedId => false;

    public override void Configure(EntityTypeBuilder<PlanningSession> builder)
    {
        base.Configure(builder);
        builder.ToTable("PlanningSessions");

        builder.Property(s => s.Mode).IsRequired()
            .HasConversion<string>();

        // SprintId and SprintName: Sprint-bound only — nullable
        builder.Property(s => s.SprintId);
        builder.Property(s => s.SprintName).HasMaxLength(200);

        // Title: required in Free mode, optional in Sprint-bound
        builder.Property(s => s.Title).HasMaxLength(200);

        // CreatedByPersonId: Identity Person id (cross-module, plain int — no FK)
        builder.Property(s => s.CreatedByPersonId).IsRequired();

        builder.Property(s => s.Status).IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.ClosedAt);

        builder.HasIndex(s => s.CreatedByPersonId);
        builder.HasIndex(s => s.Status);

        // Children: SessionParticipants owned by session.
        // UsePropertyAccessMode(Field) so EF writes into the private _participants backing field.
        builder.HasMany(s => s.Participants)
            .WithOne()
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Children: EstimationItems owned by session.
        // UsePropertyAccessMode(Field) so EF writes into the private _items backing field.
        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
