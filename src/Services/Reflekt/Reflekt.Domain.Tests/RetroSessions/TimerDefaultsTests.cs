using AwesomeAssertions;
using Reflekt.Domain.RetroSessions;
using Xunit;

namespace Reflekt.Domain.Tests.RetroSessions;

public class TimerDefaultsTests
{
    // --- Construction ---

    [Fact]
    public void Should_Create_WithValidValues()
    {
        var timers = TimerDefaults.Create(5, 3, 10);

        timers.Writing.Should().Be(5);
        timers.Voting.Should().Be(3);
        timers.Discussing.Should().Be(10);
    }

    // --- Range validation ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(100)]
    public void Should_Throw_When_WritingTimerOutOfRange(int value)
    {
        var act = () => TimerDefaults.Create(value, 3, 10);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*between 1 and 60*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Should_Throw_When_VotingTimerOutOfRange(int value)
    {
        var act = () => TimerDefaults.Create(5, value, 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Should_Throw_When_DiscussingTimerOutOfRange(int value)
    {
        var act = () => TimerDefaults.Create(5, 3, value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void Should_Accept_BoundaryValues(int value)
    {
        var act = () => TimerDefaults.Create(value, value, value);

        act.Should().NotThrow();
    }

    // --- GetForPhase ---

    [Fact]
    public void GetForPhase_Should_ReturnWriting_ForWritingPhase()
    {
        var timers = TimerDefaults.Create(5, 3, 10);

        timers.GetForPhase(RetroPhase.Writing).Should().Be(5);
    }

    [Fact]
    public void GetForPhase_Should_ReturnVoting_ForVotingPhase()
    {
        var timers = TimerDefaults.Create(5, 3, 10);

        timers.GetForPhase(RetroPhase.Voting).Should().Be(3);
    }

    [Fact]
    public void GetForPhase_Should_ReturnDiscussing_ForDiscussingPhase()
    {
        var timers = TimerDefaults.Create(5, 3, 10);

        timers.GetForPhase(RetroPhase.Discussing).Should().Be(10);
    }

    [Theory]
    [InlineData(RetroPhase.Grouping)]
    [InlineData(RetroPhase.Reporting)]
    public void GetForPhase_Should_Throw_ForNonTimerPhases(RetroPhase phase)
    {
        var timers = TimerDefaults.Create(5, 3, 10);

        var act = () => timers.GetForPhase(phase);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not have a timer default*");
    }

    // --- Value equality ---

    [Fact]
    public void Should_BeEqual_When_AllValuesMatch()
    {
        var a = TimerDefaults.Create(5, 3, 10);
        var b = TimerDefaults.Create(5, 3, 10);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Should_NotBeEqual_When_AnyValueDiffers()
    {
        var a = TimerDefaults.Create(5, 3, 10);
        var b = TimerDefaults.Create(5, 3, 15);

        a.Should().NotBe(b);
    }
}
