using AwesomeAssertions;
using Reflekt.Domain.RetroSessions;
using Xunit;

namespace Reflekt.Domain.Tests.RetroSessions;

public class RetroSessionCreateTests
{
    private const int TeamId = 1;
    private const string Name = "Retro 1";

    // --- Slice 1: empty fokusSprintIds → ArgumentException ---

    [Fact]
    public void Should_Throw_When_FokusSprintIdsIsEmpty()
    {
        var act = () => RetroSession.Create(Name, TeamId, fokusSprintIds: []);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one*");
    }

    // --- Slice 2: null fokusSprintIds → ArgumentException ---

    [Fact]
    public void Should_Throw_When_FokusSprintIdsIsNull()
    {
        var act = () => RetroSession.Create(Name, TeamId, fokusSprintIds: null!);

        act.Should().Throw<ArgumentException>();
    }

    // --- Slice 3: single fokusSprintId → one FokusSprintRef persisted ---

    [Fact]
    public void Should_CreateOneFokusSprintRef_When_OneFokusSprintIdProvided()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [42]);

        session.FokusSprintRefs.Should().HaveCount(1);
        session.FokusSprintRefs[0].FokusSprintId.Should().Be(42);
    }

    // --- Slice 4: multiple fokusSprintIds → N FokusSprintRef rows ---

    [Fact]
    public void Should_CreateNFokusSprintRefs_When_NFokusSprintIdsProvided()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [10, 20, 30]);

        session.FokusSprintRefs.Should().HaveCount(3);
        session.FokusSprintRefs.Select(r => r.FokusSprintId).Should().BeEquivalentTo([10, 20, 30]);
    }

    // --- Slice 5: newly created session starts in Draft state ---

    [Fact]
    public void Should_StartInDraftState_When_Created()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1]);

        session.State.Should().Be(RetroSessionState.Draft);
    }

    // --- Slice 6: default columns and timers are grouped into VOs ---

    [Fact]
    public void Should_HaveDefaultColumnVOs_When_Created()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1]);

        session.Column1.Name.Should().Be("Went Well");
        session.Column1.Color.Value.Should().Be("green");
        session.Column2.Name.Should().Be("Didn't Go Well");
        session.Column2.Color.Value.Should().Be("red");
        session.Column3.Name.Should().Be("Start Doing");
        session.Column3.Color.Value.Should().Be("amber");
    }

    [Fact]
    public void Should_HaveDefaultTimerVO_When_Created()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1]);

        session.Timers.Writing.Should().Be(5);
        session.Timers.Voting.Should().Be(3);
        session.Timers.Discussing.Should().Be(10);
    }

    // --- Slice 7: explicit custom columns and timers land on VOs ---

    [Fact]
    public void Should_UseCustomColumnVOs_When_ExplicitValuesProvided()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1],
            column1Name: "Win", column1Color: "blue",
            column2Name: "Loss", column2Color: "red",
            column3Name: "Try", column3Color: "amber");

        session.Column1.Name.Should().Be("Win");
        session.Column1.Color.Value.Should().Be("blue");
        session.Column2.Name.Should().Be("Loss");
        session.Column3.Name.Should().Be("Try");
    }

    [Fact]
    public void Should_UseCustomTimerVO_When_ExplicitValuesProvided()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1],
            writingTimerDefault: 10, votingTimerDefault: 5, discussingTimerDefault: 15);

        session.Timers.Writing.Should().Be(10);
        session.Timers.Voting.Should().Be(5);
        session.Timers.Discussing.Should().Be(15);
    }

    // --- Slice 8: invalid timer value via Create should throw ---

    [Fact]
    public void Should_Throw_When_TimerOutOfRange()
    {
        var act = () => RetroSession.Create(Name, TeamId, fokusSprintIds: [1], writingTimerDefault: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- Slice 9: UpdateColumnConfig replaces column VOs and raises event ---

    [Fact]
    public void UpdateColumnConfig_Should_ReplaceColumnVOs()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1]);

        session.UpdateColumnConfig("Win", "blue", "Loss", "red", "Try", "amber");

        session.Column1.Name.Should().Be("Win");
        session.Column1.Color.Value.Should().Be("blue");
        session.Column2.Name.Should().Be("Loss");
        session.Column3.Name.Should().Be("Try");
    }

    [Fact]
    public void UpdateColumnConfig_Should_RaiseColumnConfigUpdatedEvent()
    {
        var session = RetroSession.Create(Name, TeamId, fokusSprintIds: [1]);

        session.UpdateColumnConfig("Win", "blue", "Loss", "red", "Try", "amber");

        session.DomainEvents.Should().ContainSingle(e => e is Reflekt.Domain.RetroSessions.Events.ColumnConfigUpdatedEvent);
    }
}
