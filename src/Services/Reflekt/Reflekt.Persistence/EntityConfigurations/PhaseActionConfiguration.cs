using Reflekt.Domain.Sprints;

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
            new PhaseAction(SprintPhase.Writing, SprintAction.CreateNote),
            new PhaseAction(SprintPhase.Writing, SprintAction.EditNote),
            new PhaseAction(SprintPhase.Writing, SprintAction.DeleteNote),
            new PhaseAction(SprintPhase.Writing, SprintAction.AttachEvidenceLink),
            new PhaseAction(SprintPhase.Writing, SprintAction.DetachEvidenceLink),
            new PhaseAction(SprintPhase.Writing, SprintAction.AdvancePhase),
            // Voting phase
            new PhaseAction(SprintPhase.Voting, SprintAction.CastVote),
            new PhaseAction(SprintPhase.Voting, SprintAction.RetractVote),
            new PhaseAction(SprintPhase.Voting, SprintAction.AdvancePhase),
            // Discussing phase
            new PhaseAction(SprintPhase.Discussing, SprintAction.CreateActionItem),
            new PhaseAction(SprintPhase.Discussing, SprintAction.EditActionItem),
            new PhaseAction(SprintPhase.Discussing, SprintAction.DeleteActionItem),
            new PhaseAction(SprintPhase.Discussing, SprintAction.AttachEvidenceLink),
            new PhaseAction(SprintPhase.Discussing, SprintAction.DetachEvidenceLink),
            new PhaseAction(SprintPhase.Discussing, SprintAction.AdvancePhase),
            new PhaseAction(SprintPhase.Discussing, SprintAction.FocusGroup),
            new PhaseAction(SprintPhase.Discussing, SprintAction.ToggleDiscussed),
            // Grouping phase
            new PhaseAction(SprintPhase.Grouping, SprintAction.GroupCards),
            new PhaseAction(SprintPhase.Grouping, SprintAction.UngroupCards),
            new PhaseAction(SprintPhase.Grouping, SprintAction.RenameGroup),
            new PhaseAction(SprintPhase.Grouping, SprintAction.AdvancePhase)
            // Reporting: no permitted actions
        );
    }
}
