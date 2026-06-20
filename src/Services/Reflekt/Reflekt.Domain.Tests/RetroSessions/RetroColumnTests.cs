using AwesomeAssertions;
using Reflekt.Domain.RetroSessions;
using Xunit;

namespace Reflekt.Domain.Tests.RetroSessions;

public class RetroColumnTests
{
    // --- Construction ---

    [Fact]
    public void Should_Create_WithValidNameAndColor()
    {
        var col = RetroColumn.Create("Went Well", "green");

        col.Name.Should().Be("Went Well");
        col.Color.Value.Should().Be("green");
    }

    [Fact]
    public void Should_Trim_Name_OnCreate()
    {
        var col = RetroColumn.Create("  Went Well  ", "green");

        col.Name.Should().Be("Went Well");
    }

    // --- Name validation ---

    [Fact]
    public void Should_Throw_When_NameIsEmpty()
    {
        var act = () => RetroColumn.Create("", "green");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*empty or whitespace*");
    }

    [Fact]
    public void Should_Throw_When_NameIsWhitespace()
    {
        var act = () => RetroColumn.Create("   ", "green");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*empty or whitespace*");
    }

    [Fact]
    public void Should_Throw_When_NameExceeds30Chars()
    {
        var act = () => RetroColumn.Create(new string('x', 31), "green");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*30 characters*");
    }

    [Fact]
    public void Should_Accept_NameExactly30Chars()
    {
        var name = new string('x', 30);
        var col = RetroColumn.Create(name, "green");

        col.Name.Should().Be(name);
    }

    // --- Color validation (delegates to ColumnColor) ---

    [Fact]
    public void Should_Throw_When_ColorIsInvalid()
    {
        var act = () => RetroColumn.Create("Went Well", "magenta");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Throw_When_ColorIsEmpty()
    {
        var act = () => RetroColumn.Create("Went Well", "");

        act.Should().Throw<ArgumentException>();
    }

    // --- Value equality ---

    [Fact]
    public void Should_BeEqual_When_NameAndColorMatch()
    {
        var a = RetroColumn.Create("Went Well", "green");
        var b = RetroColumn.Create("Went Well", "green");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Should_NotBeEqual_When_NameDiffers()
    {
        var a = RetroColumn.Create("Went Well", "green");
        var b = RetroColumn.Create("Didn't Go Well", "green");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Should_NotBeEqual_When_ColorDiffers()
    {
        var a = RetroColumn.Create("Went Well", "green");
        var b = RetroColumn.Create("Went Well", "red");

        a.Should().NotBe(b);
    }
}
