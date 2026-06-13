using Fokus.Domain.Analytics;
using Xunit;

namespace Fokus.Domain.Tests.Analytics;

public class HealthScoreCalculatorTests
{
    // --- ScoreHigherIsBetter ---

    [Fact]
    public void ScoreHigherIsBetter_AtOrAboveGreen_Returns100()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.9m, green: 0.8m, amber: 0.5m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreHigherIsBetter_AtGreenBoundary_Returns100()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.8m, green: 0.8m, amber: 0.5m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreHigherIsBetter_MidAmberGreen_ReturnsMidRange()
    {
        // value = amber + (green-amber)/2 => midpoint => score = 50 + 49/2 ≈ 74.5
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.65m, green: 0.8m, amber: 0.5m);
        Assert.True(score > 50m && score < 100m);
    }

    [Fact]
    public void ScoreHigherIsBetter_AtAmber_Returns50()
    {
        // At amber: (value-amber)/range*49 = 0 => 50 + 0 = 50
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.5m, green: 0.8m, amber: 0.5m);
        Assert.Equal(50m, score);
    }

    [Fact]
    public void ScoreHigherIsBetter_BelowAmber_ReturnsBelowFifty()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0.25m, green: 0.8m, amber: 0.5m);
        Assert.True(score < 50m && score >= 0m);
    }

    [Fact]
    public void ScoreHigherIsBetter_Zero_ReturnsZero()
    {
        var score = HealthScoreCalculator.ScoreHigherIsBetter(value: 0m, green: 0.8m, amber: 0.5m);
        Assert.Equal(0m, score);
    }

    // --- ScoreLowerIsBetter ---

    [Fact]
    public void ScoreLowerIsBetter_AtOrBelowGreen_Returns100()
    {
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.1m, green: 0.2m, amber: 0.4m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreLowerIsBetter_AtGreenBoundary_Returns100()
    {
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.2m, green: 0.2m, amber: 0.4m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreLowerIsBetter_MidGreenAmber_ReturnsMidRange()
    {
        // value = green + (amber-green)/2 => mid => score = 99 - 49/2 ≈ 74.5
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.3m, green: 0.2m, amber: 0.4m);
        Assert.True(score > 50m && score < 100m);
    }

    [Fact]
    public void ScoreLowerIsBetter_AboveAmber_ReturnsBelowFifty()
    {
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.5m, green: 0.2m, amber: 0.4m);
        Assert.True(score < 50m && score >= 0m);
    }

    [Fact]
    public void ScoreLowerIsBetter_AtDoubleAmber_ReturnsZero()
    {
        // cap = 2*amber; value at cap => 0
        var score = HealthScoreCalculator.ScoreLowerIsBetter(value: 0.8m, green: 0.2m, amber: 0.4m);
        Assert.Equal(0m, score);
    }

    // --- CompositeRag ---

    [Fact]
    public void CompositeRag_100_ReturnsGreen() =>
        Assert.Equal("green", HealthScoreCalculator.CompositeRag(100m));

    [Fact]
    public void CompositeRag_75_ReturnsGreen() =>
        Assert.Equal("green", HealthScoreCalculator.CompositeRag(75m));

    [Fact]
    public void CompositeRag_JustBelowGreenThreshold_ReturnsAmber() =>
        Assert.Equal("amber", HealthScoreCalculator.CompositeRag(74.9m));

    [Fact]
    public void CompositeRag_40_ReturnsAmber() =>
        Assert.Equal("amber", HealthScoreCalculator.CompositeRag(40m));

    [Fact]
    public void CompositeRag_JustBelowAmberThreshold_ReturnsRed() =>
        Assert.Equal("red", HealthScoreCalculator.CompositeRag(39.9m));

    [Fact]
    public void CompositeRag_0_ReturnsRed() =>
        Assert.Equal("red", HealthScoreCalculator.CompositeRag(0m));
}
