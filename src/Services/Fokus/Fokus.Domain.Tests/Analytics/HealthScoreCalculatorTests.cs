using AwesomeAssertions;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

public class HealthScoreCalculatorTests
{
    // -----------------------------------------------------------------------
    // Slice 1: ScoreHigherIsBetter — BR1/BR2
    // -----------------------------------------------------------------------

    // BR1: value >= green → 100

    [Fact]
    public void Should_Return100_When_ValueAtOrAboveGreenThreshold()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0.9m, green: 0.8m, amber: 0.5m)
            .Should().Be(100m);
    }

    [Fact]
    public void Should_Return100_When_ValueExactlyAtGreenBoundary()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0.8m, green: 0.8m, amber: 0.5m)
            .Should().Be(100m);
    }

    // BR1: value == amber → exactly 50

    [Fact]
    public void Should_Return50_When_ValueExactlyAtAmber()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0.5m, green: 0.8m, amber: 0.5m)
            .Should().Be(50m);
    }

    // BR1: value == 0 → 0

    [Fact]
    public void Should_ReturnZero_When_ValueIsZero()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0m, green: 0.8m, amber: 0.5m)
            .Should().Be(0m);
    }

    // BR2: [amber, green) interpolates 50→99 (never reaches 100 before green)

    [Fact]
    public void Should_ReturnMidRange_When_ValueBetweenAmberAndGreen()
    {
        // midpoint between amber=0.5 and green=0.8 => value=0.65 => score in (50,100)
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.65m, green: 0.8m, amber: 0.5m);
        score.Should().BeGreaterThan(50m).And.BeLessThan(100m);
    }

    [Fact]
    public void Should_ReturnExactly99_When_ValueJustBelowGreen_InAmberBand()
    {
        // value one unit below green (use integers for exact arithmetic)
        // green=10, amber=5 — value=9 => (9-5)/(10-5)*49 + 50 = 4/5*49+50 = 39.2+50 = 89.2
        // value=9.9 => (9.9-5)/5*49+50 = 4.9/5*49+50 = 48.02+50 = 98.02 — verifies approaching but < 100
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 9.9m, green: 10m, amber: 5m);
        score.Should().BeGreaterThan(90m).And.BeLessThan(100m);
    }

    [Fact]
    public void Should_ReturnExact_LinearInterpolation_InAmberBand_HigherIsBetter()
    {
        // green=10, amber=0, value=5 => midpoint => 50 + (5-0)/(10-0)*49 = 50+24.5 = 74.5
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 5m, green: 10m, amber: 0m);
        score.Should().Be(74.5m);
    }

    // BR2: amber discontinuity — value at amber → 50; just below amber → less than 49
    // The gap exists: limit from below = 49 as value→amber⁻; at amber = 50 (jump of 1)

    [Fact]
    public void Should_Return50_AtAmber_HigherIsBetter()
    {
        // Pinning the exact amber boundary — kills mutants on the >= amber branch condition
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0.5m, green: 0.8m, amber: 0.5m)
            .Should().Be(50m);
    }

    [Fact]
    public void Should_ReturnLessThan50_JustBelowAmber_HigherIsBetter()
    {
        // value = amber - epsilon; must be < 50 (below-amber branch)
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.4999m, green: 0.8m, amber: 0.5m);
        score.Should().BeLessThan(50m);
    }

    // BR2: [0, amber) → linear 0→49

    [Fact]
    public void Should_ReturnBelowFifty_When_ValueBelowAmber()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.25m, green: 0.8m, amber: 0.5m);
        score.Should().BeLessThan(50m).And.BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Should_ReturnExact_LinearInterpolation_BelowAmber_HigherIsBetter()
    {
        // amber=10, value=5 => 5/10*49 = 24.5
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 5m, green: 20m, amber: 10m);
        score.Should().Be(24.5m);
    }

    [Fact]
    public void Should_Return0_When_ValueIsNegative_AmberIsPositive()
    {
        // Math.Max clamps negatives to 0 in the below-amber branch
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: -1m, green: 0.8m, amber: 0.5m);
        score.Should().Be(0m);
    }

    // CHARACTERIZATION TEST — candidate bug, NOT an intended rule.
    // With amber<0, the below-amber branch (value/amber*49) exceeds 49.
    // Production never passes negative thresholds; this records the actual UNGUARDED behavior.
    // See backlog note: docs/backlog.md — negative-amber pathology in ScoreHigherIsBetter.
    [Fact]
    public void Characterization_NegativeAmber_ProducesAbove49_HigherIsBetter()
    {
        // amber=-10, value=-20 => (-20)/(-10)*49 = 2*49 = 98
        // NOTE: this is a candidate BUG — the result exceeds the intended [0,49] range for the below-amber band.
        // Do NOT change this test to reflect an intended design; it pins the pathological behavior for tracking.
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: -20m, green: 5m, amber: -10m);
        score.Should().Be(98m);
    }

    // -----------------------------------------------------------------------
    // Slice 2: ScoreLowerIsBetter — BR3/BR4
    // -----------------------------------------------------------------------

    // BR3: value <= green → 100

    [Fact]
    public void Should_Return100_When_ValueAtOrBelowGreenThreshold()
    {
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.1m, green: 0.2m, amber: 0.4m)
            .Should().Be(100m);
    }

    [Fact]
    public void Should_Return100_When_ValueExactlyAtGreenBoundaryLower()
    {
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.2m, green: 0.2m, amber: 0.4m)
            .Should().Be(100m);
    }

    // BR3: value == amber → exactly 50

    [Fact]
    public void Should_Return50_When_ValueExactlyAtAmberLower()
    {
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.4m, green: 0.2m, amber: 0.4m)
            .Should().Be(50m);
    }

    // BR3: value == 2×amber → 0

    [Fact]
    public void Should_ReturnZero_When_ValueAtDoubleAmber()
    {
        // cap = 2*amber = 0.8 => score = 0
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.8m, green: 0.2m, amber: 0.4m)
            .Should().Be(0m);
    }

    // BR4: (green, amber] interpolates 99→50

    [Fact]
    public void Should_ReturnMidRange_When_ValueBetweenGreenAndAmber()
    {
        // midpoint between green=0.2 and amber=0.4 => value=0.3 => score in (50,100)
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.3m, green: 0.2m, amber: 0.4m);
        score.Should().BeGreaterThan(50m).And.BeLessThan(100m);
    }

    [Fact]
    public void Should_ReturnExact_LinearInterpolation_InGreenAmberBand_LowerIsBetter()
    {
        // green=0, amber=10, value=5 => 99 - (5-0)/(10-0)*49 = 99 - 24.5 = 74.5
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 5m, green: 0m, amber: 10m);
        score.Should().Be(74.5m);
    }

    // BR4: value just above green → approaches 99

    [Fact]
    public void Should_Return99_When_ValueJustAboveGreen_LowerIsBetter()
    {
        // value = green + tiny epsilon => approaches 99 from below
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.2001m, green: 0.2m, amber: 0.4m);
        score.Should().BeLessThan(100m).And.BeGreaterThan(98m);
    }

    // BR4: amber discontinuity — value at amber → 50; just above amber → less than 49
    // The gap exists: limit from above = 49 as value→amber⁺; at amber = 50 (jump of 1)

    [Fact]
    public void Should_Return50_AtAmber_LowerIsBetter()
    {
        // Pinning the exact amber boundary — kills mutants on the <= amber branch condition
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.4m, green: 0.2m, amber: 0.4m)
            .Should().Be(50m);
    }

    [Fact]
    public void Should_ReturnLessThan50_JustAboveAmber_LowerIsBetter()
    {
        // value = amber + epsilon; must route into above-amber branch → < 50
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.4001m, green: 0.2m, amber: 0.4m);
        score.Should().BeLessThan(50m);
    }

    // BR4: (amber, 2×amber] interpolates 49→0

    [Fact]
    public void Should_ReturnBelowFifty_When_ValueAboveAmber()
    {
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.5m, green: 0.2m, amber: 0.4m);
        score.Should().BeLessThan(50m).And.BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Should_ReturnExact_LinearInterpolation_AboveAmber_LowerIsBetter()
    {
        // amber=10, cap=20, value=15 => 49 - (15-10)/(20-10)*49 = 49 - 24.5 = 24.5
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 15m, green: 0m, amber: 10m);
        score.Should().Be(24.5m);
    }

    // BR4: values above 2×amber clamped to 0

    [Fact]
    public void Should_ReturnZero_When_ValueExceedsDoubleAmber()
    {
        // amber=0.4, cap=0.8 — value=1.0 (above cap) => Math.Max clamps to 0
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 1.0m, green: 0.2m, amber: 0.4m);
        score.Should().Be(0m);
    }

    // cap <= amber guard (L42): amber=0 makes cap=0, so cap<=amber is 0<=0 (true) → return 0.
    // Mutant "cap < amber" would be 0<0 (false) and fall through to divide-by-zero territory.
    // This pins the amber==0 boundary for the above-amber branch.
    [Fact]
    public void Should_ReturnZero_When_AmberIsZero_AboveAmberBranch_LowerIsBetter()
    {
        // value=1, green=-1, amber=0 → cap=0, cap<=amber → returns 0
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 1m, green: -1m, amber: 0m);
        score.Should().Be(0m);
    }

    // -----------------------------------------------------------------------
    // Slice 3: CompositeRag — BR5
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnGreen_When_ScoreIs100() =>
        HealthScoreCalculator.CompositeRag(100m).Should().Be("green");

    [Fact]
    public void Should_ReturnGreen_When_ScoreIsAtGreenThreshold() =>
        HealthScoreCalculator.CompositeRag(75m).Should().Be("green");

    [Fact]
    public void Should_ReturnAmber_When_ScoreJustBelowGreenThreshold() =>
        HealthScoreCalculator.CompositeRag(74.9m).Should().Be("amber");

    [Fact]
    public void Should_ReturnAmber_When_ScoreIsAtAmberThreshold() =>
        HealthScoreCalculator.CompositeRag(40m).Should().Be("amber");

    [Fact]
    public void Should_ReturnRed_When_ScoreJustBelowAmberThreshold() =>
        HealthScoreCalculator.CompositeRag(39.9m).Should().Be("red");

    [Fact]
    public void Should_ReturnRed_When_ScoreIsZero() =>
        HealthScoreCalculator.CompositeRag(0m).Should().Be("red");

    // -----------------------------------------------------------------------
    // Slice 4: MetricRag — BR6 (higher is better)
    // -----------------------------------------------------------------------

    // BR6: value >= green → "green"

    [Fact]
    public void Should_ReturnGreen_When_ValueAtGreen_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.8m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("green");
    }

    [Fact]
    public void Should_ReturnGreen_When_ValueAboveGreen_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.95m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("green");
    }

    // BR6: value >= amber (but < green) → "amber"

    [Fact]
    public void Should_ReturnAmber_When_ValueAtAmber_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.5m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("amber");
    }

    [Fact]
    public void Should_ReturnAmber_When_ValueBetweenAmberAndGreen_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.65m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("amber");
    }

    // BR6: value < amber → "red"

    [Fact]
    public void Should_ReturnRed_When_ValueBelowAmber_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.3m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("red");
    }

    [Fact]
    public void Should_ReturnRed_When_ValueIsZero_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("red");
    }

    // BR6 boundary: just below amber → "red"; at amber → "amber" (inclusive on better side)

    [Fact]
    public void Should_ReturnRed_JustBelowAmber_HigherIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.4999m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("red");
    }

    [Fact]
    public void Should_ReturnGreen_JustBelowGreen_IsNotGreen_HigherIsBetter_MetricRag()
    {
        // value just below green → amber, not green — kills >= vs > mutant on green check
        HealthScoreCalculator.MetricRag(value: 0.7999m, green: 0.8m, amber: 0.5m, higherIsBetter: true)
            .Should().Be("amber");
    }

    // -----------------------------------------------------------------------
    // Slice 5: MetricRag — BR7 (lower is better)
    // -----------------------------------------------------------------------

    // BR7: value <= green → "green"

    [Fact]
    public void Should_ReturnGreen_When_ValueAtGreen_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.2m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("green");
    }

    [Fact]
    public void Should_ReturnGreen_When_ValueBelowGreen_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.1m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("green");
    }

    // BR7: value <= amber (but > green) → "amber"

    [Fact]
    public void Should_ReturnAmber_When_ValueAtAmber_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.4m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("amber");
    }

    [Fact]
    public void Should_ReturnAmber_When_ValueBetweenGreenAndAmber_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.3m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("amber");
    }

    // BR7: value > amber → "red"

    [Fact]
    public void Should_ReturnRed_When_ValueAboveAmber_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.6m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("red");
    }

    [Fact]
    public void Should_ReturnRed_When_ValueIsHigh_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 1.0m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("red");
    }

    // BR7 boundary: just above amber → "red"; at amber → "amber" (inclusive on better side)

    [Fact]
    public void Should_ReturnRed_JustAboveAmber_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.4001m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("red");
    }

    [Fact]
    public void Should_ReturnAmber_JustAboveGreen_IsNotGreen_LowerIsBetter_MetricRag()
    {
        // value just above green → amber, not green — kills <= vs < mutant on green check
        HealthScoreCalculator.MetricRag(value: 0.2001m, green: 0.2m, amber: 0.4m, higherIsBetter: false)
            .Should().Be("amber");
    }

    // BR7: higherIsBetter=false with green=amber (degenerate) — green-exact still routes green
    [Fact]
    public void Should_ReturnGreen_When_GreenEqualsAmber_AtGreen_LowerIsBetter_MetricRag()
    {
        HealthScoreCalculator.MetricRag(value: 0.3m, green: 0.3m, amber: 0.3m, higherIsBetter: false)
            .Should().Be("green");
    }

    // Verify higherIsBetter flag is actually observed (not hardcoded to one path).
    // value=0.3, green=0.8, amber=0.5:
    //   higher-is-better: 0.3 < amber(0.5) → "red"
    //   lower-is-better:  0.3 <= green(0.8) → "green"
    // Same value, same thresholds, opposite directions → different outcomes.
    [Fact]
    public void Should_DifferByDirection_ForSameThresholds()
    {
        var whenHigherIsBetter = HealthScoreCalculator.MetricRag(value: 0.3m, green: 0.8m, amber: 0.5m, higherIsBetter: true);
        var whenLowerIsBetter  = HealthScoreCalculator.MetricRag(value: 0.3m, green: 0.8m, amber: 0.5m, higherIsBetter: false);
        whenHigherIsBetter.Should().Be("red");
        whenLowerIsBetter.Should().Be("green");
    }
}
