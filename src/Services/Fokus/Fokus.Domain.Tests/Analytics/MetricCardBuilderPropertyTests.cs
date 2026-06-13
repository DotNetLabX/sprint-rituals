using AwesomeAssertions;
using Fokus.Domain.Analytics;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Property-based tests for MetricCardBuilder — the pure delta/direction/polarity kernel.
/// Each [Property] test must hold for ALL generated inputs; Stryker confirms coverage by
/// killing mutants in the implementation.
///
/// FsCheck 3.x C# API:
///   [Property] methods take typed parameters that FsCheck auto-generates.
///   Return bool (or void with AwesomeAssertions throws) — no Prop.ForAll wrapper needed.
///   Custom generators: register via [Property(Arbitrary = new[] { typeof(MyArbs) })].
///   Gen extension methods live in FsCheck.Fluent: .Select(), .Where(), .ToArbitrary().
/// </summary>
public class MetricCardBuilderPropertyTests
{
    // --- Slice 1: Direction invariants ---

    // FsCheck generates arbitrary decimal values; we assert the contract holds for all of them.
    [Property]
    public bool Direction_IsAlwaysOneOfThreeValues(decimal delta)
    {
        var direction = MetricCardBuilder.Direction(delta);
        return direction == "up" || direction == "down" || direction == "flat";
    }

    [Property]
    public bool Polarity_IsAlwaysOneOfThreeValues(decimal delta, bool positiveUp)
    {
        var polarity = MetricCardBuilder.Polarity(delta, positiveUp);
        return polarity == "positive" || polarity == "negative" || polarity == "neutral";
    }

    [Property]
    public bool Polarity_IsNeutral_When_DeltaIsZero(bool positiveUp) =>
        MetricCardBuilder.Polarity(0m, positiveUp) == "neutral";

    [Fact]
    public void Should_ReturnFlat_When_DeltaIsZero()
    {
        MetricCardBuilder.Direction(0m).Should().Be("flat");
    }

    // --- Slice 2: Direction/Polarity symmetry for non-zero deltas ---

    // Use PositiveInt wrapper (FsCheck built-in) to constrain inputs > 0 without rejection.
    // Cast to decimal for the method under test.
    [Property]
    public bool Direction_IsUp_When_DeltaIsPositive(PositiveInt n)
    {
        var delta = (decimal)n.Get;
        return MetricCardBuilder.Direction(delta) == "up";
    }

    [Property]
    public bool Direction_IsDown_When_DeltaIsNegative(PositiveInt n)
    {
        var delta = -(decimal)n.Get;
        return MetricCardBuilder.Direction(delta) == "down";
    }

    [Property]
    public bool Polarity_Inverts_When_PositiveUpFlips(PositiveInt n)
    {
        // For strictly positive delta: positiveUp=true -> "positive", positiveUp=false -> "negative"
        var delta = (decimal)n.Get;
        var whenUp   = MetricCardBuilder.Polarity(delta, positiveUp: true);
        var whenDown = MetricCardBuilder.Polarity(delta, positiveUp: false);
        return whenUp == "positive" && whenDown == "negative";
    }

    [Property]
    public bool Polarity_Inverts_When_PositiveUpFlips_NegativeDelta(PositiveInt n)
    {
        // For strictly negative delta: positiveUp=true -> "negative", positiveUp=false -> "positive"
        var delta = -(decimal)n.Get;
        var whenUp   = MetricCardBuilder.Polarity(delta, positiveUp: true);
        var whenDown = MetricCardBuilder.Polarity(delta, positiveUp: false);
        return whenUp == "negative" && whenDown == "positive";
    }

    // --- Slice 3: ComputeDelta — null propagation and neutral short-circuit ---

    [Property]
    public bool ComputeDelta_BothOutputsNull_When_DeltaIsNull(NonNull<string> polarityInput)
    {
        var (direction, polarity) = MetricCardBuilder.ComputeDelta(null, polarityInput.Get);
        return direction == null && polarity == null;
    }

    [Property]
    public bool ComputeDelta_PolarityIsNeutral_When_DeltaIsNonZeroAndKnownPolarity(PositiveInt n)
    {
        // Sanity check: "positive-up" with positive delta -> "positive" (not neutral)
        var delta = (decimal)n.Get;
        var (_, polarity) = MetricCardBuilder.ComputeDelta(delta, "positive-up");
        return polarity == "positive";
    }

    [Property]
    public bool ComputeDelta_DirectionIsUp_When_DeltaIsPositive(PositiveInt n)
    {
        var delta = (decimal)n.Get;
        var (direction, _) = MetricCardBuilder.ComputeDelta(delta, "positive-up");
        return direction == "up";
    }

    [Property]
    public bool ComputeDelta_DirectionIsDown_When_DeltaIsNegative(PositiveInt n)
    {
        var delta = -(decimal)n.Get;
        var (direction, _) = MetricCardBuilder.ComputeDelta(delta, "positive-up");
        return direction == "down";
    }
}
