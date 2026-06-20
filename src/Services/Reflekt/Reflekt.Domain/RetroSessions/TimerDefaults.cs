using Blocks.Domain.ValueObjects;

namespace Reflekt.Domain.RetroSessions;

public sealed class TimerDefaults : ValueObject
{
    private const int Min = 1;
    private const int Max = 60;

    public int Writing { get; private set; }
    public int Voting { get; private set; }
    public int Discussing { get; private set; }

    // Parameterless ctor required by EF Core ComplexProperty materialization
    private TimerDefaults() { }

    private TimerDefaults(int writing, int voting, int discussing)
    {
        Writing = writing;
        Voting = voting;
        Discussing = discussing;
    }

    /// <summary>Creates validated timer defaults. Each value must be between 1 and 60 (inclusive).</summary>
    public static TimerDefaults Create(int writing, int voting, int discussing)
    {
        ValidateRange(writing, nameof(writing));
        ValidateRange(voting, nameof(voting));
        ValidateRange(discussing, nameof(discussing));
        return new TimerDefaults(writing, voting, discussing);
    }

    /// <summary>Returns the timer default for the given phase. Throws for non-timer phases.</summary>
    public int GetForPhase(RetroPhase phase) => phase switch
    {
        RetroPhase.Writing => Writing,
        RetroPhase.Voting => Voting,
        RetroPhase.Discussing => Discussing,
        _ => throw new InvalidOperationException($"Phase {phase} does not have a timer default."),
    };

    private static void ValidateRange(int value, string paramName)
    {
        if (value < Min || value > Max)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Timer default must be between {Min} and {Max} minutes.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Writing;
        yield return Voting;
        yield return Discussing;
    }
}
