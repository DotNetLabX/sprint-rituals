using AwesomeAssertions;
using Poker.Domain.Planning.Items;
using Xunit;

namespace Poker.Domain.Tests.Planning;

public class EstimationItemVoteTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid ParticipantA = Guid.NewGuid();
    private static readonly Guid ParticipantB = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Now.AddMinutes(1);

    // --- Slice 1: Rule 21 — second CastVote by same participant replaces, not appends ---

    [Fact]
    public void Should_ReplaceVote_When_SameParticipantVotesTwice()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);

        item.CastVote(ParticipantA, "3", Now);
        item.CastVote(ParticipantA, "5", Later);

        item.Votes.Should().HaveCount(1);
        item.Votes[0].Value.Should().Be("5");
    }

    // --- Slice 2: Two different participants each get one vote ---

    [Fact]
    public void Should_HaveTwoVotes_When_TwoParticipantsVote()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);

        item.CastVote(ParticipantA, "5", Now);
        item.CastVote(ParticipantB, "8", Now);

        item.Votes.Should().HaveCount(2);
    }

    // --- Slice 3: Invalid Fibonacci value is rejected ---

    [Fact]
    public void Should_Throw_When_InvalidFibonacciValue()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);

        var act = () => item.CastVote(ParticipantA, "4", Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid Fibonacci*");
    }

    // --- Slice 4: AllPresentVotersHaveVoted returns false when a voter hasn't voted ---

    [Fact]
    public void Should_ReturnFalse_When_NotAllConnectedVotersHaveVoted()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "5", Now);

        var connectedVoters = new HashSet<Guid> { ParticipantA, ParticipantB };

        item.AllPresentVotersHaveVoted(connectedVoters).Should().BeFalse();
    }

    // --- Slice 5: AllPresentVotersHaveVoted returns true when all have voted ---

    [Fact]
    public void Should_ReturnTrue_When_AllConnectedVotersHaveVoted()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "5", Now);
        item.CastVote(ParticipantB, "8", Now);

        var connectedVoters = new HashSet<Guid> { ParticipantA, ParticipantB };

        item.AllPresentVotersHaveVoted(connectedVoters).Should().BeTrue();
    }

    // --- Slice 6: Cannot cast vote after reveal ---

    [Fact]
    public void Should_Throw_When_VotingAfterReveal()
    {
        var item = EstimationItem.Create(SessionId, "Story", order: 1);
        item.CastVote(ParticipantA, "5", Now);
        item.Reveal();

        var act = () => item.CastVote(ParticipantB, "8", Later);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*after votes have been revealed*");
    }
}
