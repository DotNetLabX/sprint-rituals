using AwesomeAssertions;
using Poker.Domain.Planning.Items;
using Xunit;

namespace Poker.Domain.Tests.Planning;

public class EstimationItemSkipTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid ParticipantA = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    // --- Slice 1: Rule 9 — Skip works from Voting (no votes cast) ---

    [Fact]
    public void Should_MarkSkipped_When_NoVotesCast()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);

        item.Skip();

        item.Status.Should().Be(ItemStatus.Skipped);
        item.FinalEstimate.Should().BeNull();
    }

    // --- Slice 2: Rule 9 — Skip works when all votes are "?" ---

    [Fact]
    public void Should_MarkSkipped_When_AllVotesAreAbstain()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "?", Now);
        item.Reveal();

        item.Skip();

        item.Status.Should().Be(ItemStatus.Skipped);
    }

    // --- Slice 3: Rule 9 — Skip works from Revealed status ---

    [Fact]
    public void Should_MarkSkipped_When_ItemIsRevealed()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "5", Now);
        item.Reveal();

        item.Skip();

        item.Status.Should().Be(ItemStatus.Skipped);
    }

    // --- Slice 4: Cannot skip an already-Decided item ---

    [Fact]
    public void Should_Throw_When_SkippingDecidedItem()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "5", Now);
        item.Reveal();
        item.Decide("5", Now);

        var act = () => item.Skip();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Decided*");
    }
}
