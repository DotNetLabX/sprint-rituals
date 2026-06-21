using AwesomeAssertions;
using Poker.Domain.Planning.Items;
using Xunit;

namespace Poker.Domain.Tests.Planning;

public class EstimationItemDecideTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid ParticipantA = Guid.NewGuid();
    private static readonly Guid ParticipantB = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    private static EstimationItem RevealedItemWith(params (Guid participant, string value)[] votes)
    {
        var item = EstimationItem.Create(SessionId, "FOK-1: Some ticket", order: 1, ticketKey: "FOK-1");
        foreach (var (pid, val) in votes)
            item.CastVote(pid, val, Now);
        item.Reveal();
        return item;
    }

    // --- Slice 1: Rule 6 — "?" is never the final estimate ---

    [Fact]
    public void Should_Throw_When_DecidingWithAbstain()
    {
        var item = RevealedItemWith((ParticipantA, "5"), (ParticipantB, "?"));

        var act = () => item.Decide("?", Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be the final estimate*");
    }

    // --- Slice 2: Rule 8 — value must be one of the values actually cast ---

    [Fact]
    public void Should_Throw_When_DecidingWithUncastValue()
    {
        var item = RevealedItemWith((ParticipantA, "3"), (ParticipantB, "5"));

        var act = () => item.Decide("8", Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*was not cast*");
    }

    // --- Slice 3: Valid decide transitions to Decided ---

    [Fact]
    public void Should_SetFinalEstimate_When_ValidValueDecided()
    {
        var item = RevealedItemWith((ParticipantA, "5"), (ParticipantB, "5"));

        item.Decide("5", Now);

        item.Status.Should().Be(ItemStatus.Decided);
        item.FinalEstimate.Should().Be("5");
        item.DecidedAt.Should().Be(Now);
    }

    // --- Slice 4: Rule 6 (extended) — all-"?" round cannot be decided ---

    [Fact]
    public void Should_Throw_When_AllVotesAreAbstain()
    {
        var item = RevealedItemWith((ParticipantA, "?"), (ParticipantB, "?"));

        var act = () => item.Decide("5", Now);

        // "5" is not in the cast values (only "?" was cast)
        act.Should().Throw<ArgumentException>()
            .WithMessage("*was not cast*");
    }

    // --- Slice 5: Cannot decide before reveal ---

    [Fact]
    public void Should_Throw_When_DecidingBeforeReveal()
    {
        var item = EstimationItem.Create(SessionId, "FOK-2: Task", order: 1, ticketKey: "FOK-2");
        item.CastVote(ParticipantA, "5", Now);

        var act = () => item.Decide("5", Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revealed*");
    }
}
