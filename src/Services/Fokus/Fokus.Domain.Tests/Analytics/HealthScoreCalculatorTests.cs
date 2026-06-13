using AwesomeAssertions;
using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

public class HealthScoreCalculatorTests
{
    // --- Slice 1: ScoreHigherIsBetter — boundary and range ---

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

    [Fact]
    public void Should_ReturnMidRange_When_ValueBetweenAmberAndGreen()
    {
        // midpoint between amber=0.5 and green=0.8 => value=0.65 => score in (50,100)
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.65m, green: 0.8m, amber: 0.5m);
        score.Should().BeGreaterThan(50m).And.BeLessThan(100m);
    }

    [Fact]
    public void Should_Return50_When_ValueExactlyAtAmber()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0.5m, green: 0.8m, amber: 0.5m)
            .Should().Be(50m);
    }

    [Fact]
    public void Should_ReturnBelowFifty_When_ValueBelowAmber()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.25m, green: 0.8m, amber: 0.5m);
        score.Should().BeLessThan(50m).And.BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Should_ReturnZero_When_ValueIsZero()
    {
        HealthScoreCalculator.ScoreHigherIsBetter(value: 0m, green: 0.8m, amber: 0.5m)
            .Should().Be(0m);
    }

    // --- Slice 2: ScoreLowerIsBetter — boundary and range ---

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

    [Fact]
    public void Should_ReturnMidRange_When_ValueBetweenGreenAndAmber()
    {
        // midpoint between green=0.2 and amber=0.4 => value=0.3 => score in (50,100)
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.3m, green: 0.2m, amber: 0.4m);
        score.Should().BeGreaterThan(50m).And.BeLessThan(100m);
    }

    [Fact]
    public void Should_ReturnBelowFifty_When_ValueAboveAmber()
    {
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.5m, green: 0.2m, amber: 0.4m);
        score.Should().BeLessThan(50m).And.BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Should_ReturnZero_When_ValueAtDoubleAmber()
    {
        // cap = 2*amber = 0.8 => score = 0
        HealthScoreCalculator.ScoreLowerIsBetter(value: 0.8m, green: 0.2m, amber: 0.4m)
            .Should().Be(0m);
    }

    // --- Slice 3: CompositeRag — thresholds ---

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
}
