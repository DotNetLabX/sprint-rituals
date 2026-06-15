namespace Reflekt.Domain.RetroSessions;

public sealed class PhaseAction
{
    private PhaseAction() { }

    public PhaseAction(RetroPhase phase, RetroAction action)
    {
        Phase = phase;
        Action = action;
    }

    public RetroPhase Phase { get; private set; }
    public RetroAction Action { get; private set; }
}
