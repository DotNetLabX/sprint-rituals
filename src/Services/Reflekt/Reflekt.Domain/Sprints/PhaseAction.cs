namespace Reflekt.Domain.Sprints;

public sealed class PhaseAction
{
    private PhaseAction() { }

    public PhaseAction(SprintPhase phase, SprintAction action)
    {
        Phase = phase;
        Action = action;
    }

    public SprintPhase Phase { get; private set; }
    public SprintAction Action { get; private set; }
}
