using AwesomeAssertions;
using Reflekt.Domain.RetroSessions;
using Xunit;

namespace Reflekt.Domain.Tests.RetroSessions;

public class RetroSessionCreateTests
{
    private const int TeamId = 1;
    private const string Name = "Sprint 1 Retro";

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
}
