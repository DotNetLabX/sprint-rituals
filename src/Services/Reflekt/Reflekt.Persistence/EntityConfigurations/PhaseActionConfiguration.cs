using Reflekt.Domain.RetroSessions;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class PhaseActionConfiguration : IEntityTypeConfiguration<PhaseAction>
{
    public void Configure(EntityTypeBuilder<PhaseAction> builder)
    {
        builder.ToTable("PhaseActions");
        builder.HasKey(x => new { x.Phase, x.Action });
        builder.Property(x => x.Phase).IsRequired();
        builder.Property(x => x.Action).IsRequired();

        builder.HasData(
            // Writing phase
            new PhaseAction(RetroPhase.Writing, RetroAction.CreateNote),
            new PhaseAction(RetroPhase.Writing, RetroAction.EditNote),
            new PhaseAction(RetroPhase.Writing, RetroAction.DeleteNote),
            new PhaseAction(RetroPhase.Writing, RetroAction.AttachEvidenceLink),
            new PhaseAction(RetroPhase.Writing, RetroAction.DetachEvidenceLink),
            new PhaseAction(RetroPhase.Writing, RetroAction.AdvancePhase),
            // Voting phase
            new PhaseAction(RetroPhase.Voting, RetroAction.CastVote),
            new PhaseAction(RetroPhase.Voting, RetroAction.RetractVote),
            new PhaseAction(RetroPhase.Voting, RetroAction.AdvancePhase),
            // Discussing phase
            new PhaseAction(RetroPhase.Discussing, RetroAction.CreateActionItem),
            new PhaseAction(RetroPhase.Discussing, RetroAction.EditActionItem),
            new PhaseAction(RetroPhase.Discussing, RetroAction.DeleteActionItem),
            new PhaseAction(RetroPhase.Discussing, RetroAction.AttachEvidenceLink),
            new PhaseAction(RetroPhase.Discussing, RetroAction.DetachEvidenceLink),
            new PhaseAction(RetroPhase.Discussing, RetroAction.AdvancePhase),
            new PhaseAction(RetroPhase.Discussing, RetroAction.FocusGroup),
            new PhaseAction(RetroPhase.Discussing, RetroAction.ToggleDiscussed),
            // Grouping phase
            new PhaseAction(RetroPhase.Grouping, RetroAction.GroupCards),
            new PhaseAction(RetroPhase.Grouping, RetroAction.UngroupCards),
            new PhaseAction(RetroPhase.Grouping, RetroAction.RenameGroup),
            new PhaseAction(RetroPhase.Grouping, RetroAction.AdvancePhase)
            // Reporting: no permitted actions
        );
    }
}

