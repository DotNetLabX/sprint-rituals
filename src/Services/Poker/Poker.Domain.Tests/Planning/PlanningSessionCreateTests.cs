using AwesomeAssertions;
using Poker.Domain.Planning;
using Xunit;

namespace Poker.Domain.Tests.Planning;

public class PlanningSessionCreateTests
{
    private const int SmPersonId = 1;
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    // --- Slice 1: Sprint-bound requires a sprintName ---

    [Fact]
    public void Should_Throw_When_SprintBoundCreatedWithNullSprintName()
    {
        var act = () => PlanningSession.CreateSprintBound(SmPersonId, sprintId: 42, sprintName: null!, title: null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Throw_When_SprintBoundCreatedWithEmptySprintName()
    {
        var act = () => PlanningSession.CreateSprintBound(SmPersonId, sprintId: 42, sprintName: "", title: null, Now);

        act.Should().Throw<ArgumentException>();
    }

    // --- Slice 2: Free mode requires a title ---

    [Fact]
    public void Should_Throw_When_FreeCreatedWithNullTitle()
    {
        var act = () => PlanningSession.CreateFree(SmPersonId, title: null!, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Throw_When_FreeCreatedWithEmptyTitle()
    {
        var act = () => PlanningSession.CreateFree(SmPersonId, title: "", Now);

        act.Should().Throw<ArgumentException>();
    }

    // --- Slice 3: Sprint-bound session starts Active ---

    [Fact]
    public void Should_StartActive_When_SprintBoundCreated()
    {
        var session = PlanningSession.CreateSprintBound(SmPersonId, 42, "Sprint 1", title: null, Now);

        session.Status.Should().Be(SessionStatus.Active);
        session.Mode.Should().Be(SessionMode.SprintBound);
        session.SprintId.Should().Be(42);
        session.SprintName.Should().Be("Sprint 1");
    }

    // --- Slice 4: Free session starts Active with title and no sprint ---

    [Fact]
    public void Should_StartActive_When_FreeCreated()
    {
        var session = PlanningSession.CreateFree(SmPersonId, "Ad-hoc estimation", Now);

        session.Status.Should().Be(SessionStatus.Active);
        session.Mode.Should().Be(SessionMode.Free);
        session.Title.Should().Be("Ad-hoc estimation");
        session.SprintId.Should().BeNull();
    }
}
