
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class RetroSessionConfiguration : EntityConfiguration<RetroSession>
{
    public override void Configure(EntityTypeBuilder<RetroSession> builder)
    {
        base.Configure(builder);
        builder.ToTable("RetroSessions");
        builder.Property(s => s.TeamId).IsRequired();
        builder.HasOne<Team>().WithMany().HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.State).IsRequired();
        builder.Property(s => s.CurrentPhase).IsRequired();
        builder.Property(s => s.VotesPerParticipant).IsRequired().HasDefaultValue(3);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.FocusedGroupId);
        builder.HasIndex(s => s.TeamId);

        builder.ComplexProperty(s => s.Column1, b =>
        {
            b.Property(c => c.Name).IsRequired().HasMaxLength(30).HasDefaultValue("Went Well");
            b.Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue(ColumnColor.Create("green"))
                .HasConversion(c => c.Value, s => ColumnColor.Create(s));
        });

        builder.ComplexProperty(s => s.Column2, b =>
        {
            b.Property(c => c.Name).IsRequired().HasMaxLength(30).HasDefaultValue("Didn't Go Well");
            b.Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue(ColumnColor.Create("red"))
                .HasConversion(c => c.Value, s => ColumnColor.Create(s));
        });

        builder.ComplexProperty(s => s.Column3, b =>
        {
            b.Property(c => c.Name).IsRequired().HasMaxLength(30).HasDefaultValue("Start Doing");
            b.Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue(ColumnColor.Create("amber"))
                .HasConversion(c => c.Value, s => ColumnColor.Create(s));
        });

        builder.ComplexProperty(s => s.Timers, b =>
        {
            b.Property(t => t.Writing).IsRequired().HasDefaultValue(5);
            b.Property(t => t.Voting).IsRequired().HasDefaultValue(3);
            b.Property(t => t.Discussing).IsRequired().HasDefaultValue(10);
        });

        builder.HasIndex(s => s.ClosedAt);

        // 1..N Fokus sprint link (child entity, composite key) — per EvidenceLink pattern
        builder.HasMany(s => s.FokusSprintRefs)
            .WithOne()
            .HasForeignKey(r => r.RetroSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.FokusSprintRefs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
