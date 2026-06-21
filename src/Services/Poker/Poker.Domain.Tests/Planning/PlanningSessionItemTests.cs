using AwesomeAssertions;
using Poker.Domain.Planning;
using Xunit;

namespace Poker.Domain.Tests.Planning;

public class PlanningSessionItemTests
{
    private const int SmPersonId = 1;
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    private static PlanningSession SprintBoundSession()
        => PlanningSession.CreateSprintBound(SmPersonId, sprintId: 42, sprintName: "Sprint 1", title: null, Now);

    // --- Slice 1: Rule 19 — re-adding a removed ticket reuses existing item ---

    [Fact]
    public void Should_ReuseExistingItem_When_TicketKeyAlreadyInSession()
    {
        var session = SprintBoundSession();
        var first = session.AddSprintBoundItem("FOK-1", "FOK-1: Story A", order: 1);

        // "Re-add" the same ticket (e.g. refresh after it was briefly removed then re-added)
        var second = session.AddSprintBoundItem("FOK-1", "FOK-1: Story A (refreshed)", order: 1);

        second.Should().BeSameAs(first, "the existing item is reused, not duplicated");
        session.Items.Should().HaveCount(1);
    }

    // --- Slice 2: Different ticket keys create separate items ---

    [Fact]
    public void Should_CreateSeparateItems_When_DifferentTicketKeys()
    {
        var session = SprintBoundSession();

        session.AddSprintBoundItem("FOK-1", "Story A", order: 1);
        session.AddSprintBoundItem("FOK-2", "Story B", order: 2);

        session.Items.Should().HaveCount(2);
    }

    // --- Slice 3: Cannot add sprint-bound item to free session ---

    [Fact]
    public void Should_Throw_When_AddingSprintBoundItemToFreeSession()
    {
        var session = PlanningSession.CreateFree(SmPersonId, "Ad-hoc", Now);

        var act = () => session.AddSprintBoundItem("FOK-1", "Story A", order: 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sprint-bound*");
    }

    // --- Slice 4: Cannot add free item to sprint-bound session ---

    [Fact]
    public void Should_Throw_When_AddingFreeItemToSprintBoundSession()
    {
        var session = SprintBoundSession();

        var act = () => session.AddFreeItem("Ad-hoc item", order: 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Free-mode*");
    }

    // --- Slice 5: Close raises SessionClosedEvent with correct stats ---

    [Fact]
    public void Should_RaiseSessionClosedEvent_When_Closed()
    {
        var session = SprintBoundSession();
        var item1 = session.AddSprintBoundItem("FOK-1", "Story A", order: 1);
        var item2 = session.AddSprintBoundItem("FOK-2", "Story B", order: 2);

        item1.CastVote(Guid.NewGuid(), "5", Now);
        item1.Reveal();
        item1.Decide("5", Now);
        item2.Skip();

        session.Close(Now);

        session.Status.Should().Be(SessionStatus.Closed);
        session.DomainEvents.Should().ContainSingle(e => e is Poker.Domain.Planning.Events.SessionClosedEvent);

        var evt = (Poker.Domain.Planning.Events.SessionClosedEvent)session.DomainEvents
            .Single(e => e is Poker.Domain.Planning.Events.SessionClosedEvent);
        evt.ItemsEstimated.Should().Be(1);
        evt.ItemsSkipped.Should().Be(1);
        evt.TotalStoryPoints.Should().Be(5m);
    }
}
