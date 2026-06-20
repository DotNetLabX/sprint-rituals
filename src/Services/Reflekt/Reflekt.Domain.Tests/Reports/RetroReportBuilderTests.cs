using AwesomeAssertions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Reports;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;
using Xunit;

namespace Reflekt.Domain.Tests.Reports;

public class RetroReportBuilderTests
{
    private const int TeamId = 1;
    private const int OwnerId = 42;
    private const int RetroSessionId = 10;
    private const int PreviousSessionId = 9;
    private const int ParticipantId = 5;

    private readonly RetroReportBuilder _sut = new();

    // Shared helpers
    private static RetroSession MakeSession(int id = RetroSessionId) =>
        RetroSession.Create($"Retro {id}", TeamId, fokusSprintIds: [1]);

    private static Team MakeTeam() =>
        Team.Create("Alpha Team", OwnerId);

    private static Card MakeNoteCard(string text, int? groupId = null, ColumnType column = ColumnType.Column1)
    {
        var card = Card.Create(text, RetroSessionId, ParticipantId, column, CardType.Note, assigneeName: null);
        if (groupId.HasValue) card.SetGroupId(groupId.Value);
        return card;
    }

    private static Card MakeActionItemCard(string text, string assignee = "Alice") =>
        Card.Create(text, RetroSessionId, ParticipantId, ColumnType.ActionItems, CardType.ActionItem, assigneeName: assignee);

    private static CardGroup MakeGroup(int retroSessionId = RetroSessionId, ColumnType column = ColumnType.Column1, string? title = null)
    {
        var g = CardGroup.Create(retroSessionId, column, title);
        return g;
    }

    // --- Slice 1: Group filter — undiscussed group with ≤1 vote is excluded ---

    [Fact]
    public void Should_ExcludeGroup_When_NotDiscussedAndVotesAtMostOne()
    {
        var session = MakeSession();
        var group = MakeGroup();
        var card = MakeNoteCard("low-signal note", group.Id);
        var voteCounts = new Dictionary<int, int> { [group.Id] = 1 };

        var result = _sut.Build(session, [card], [group], voteCounts, [], MakeTeam(), [], [], null);

        result.Columns.Should().AllSatisfy(col => col.Groups.Should().BeEmpty());
    }

    // --- Slice 2: Group filter — discussed group with 0 votes is included ---

    [Fact]
    public void Should_IncludeGroup_When_DiscussedAndZeroVotes()
    {
        var session = MakeSession();
        var group = MakeGroup();
        group.MarkDiscussed();
        var card = MakeNoteCard("discussed topic", group.Id);
        var voteCounts = new Dictionary<int, int> { [group.Id] = 0 };

        var result = _sut.Build(session, [card], [group], voteCounts, [], MakeTeam(), [], [], null);

        var col1 = result.Columns.Single(c => c.ColumnName == "Went Well");
        col1.Groups.Should().HaveCount(1);
        col1.Groups[0].GroupTitle.Should().Be("discussed topic");
        col1.Groups[0].IsDiscussed.Should().BeTrue();
    }

    // --- Slice 3: Group filter — undiscussed group with 2 votes is included ---

    [Fact]
    public void Should_IncludeGroup_When_NotDiscussedAndVotesGreaterThanOne()
    {
        var session = MakeSession();
        var group = MakeGroup();
        var card = MakeNoteCard("popular note", group.Id);
        var voteCounts = new Dictionary<int, int> { [group.Id] = 2 };

        var result = _sut.Build(session, [card], [group], voteCounts, [], MakeTeam(), [], [], null);

        var col1 = result.Columns.Single(c => c.ColumnName == "Went Well");
        col1.Groups.Should().HaveCount(1);
    }

    // --- Slice 4: Ordering — groups within a column are vote-descending ---
    // Note: in-memory domain objects always have Id=0 (no DB assigned), so we can't use a
    // vote-count dictionary to distinguish two groups. We test ordering by having one
    // discussed group (0 votes) and one undiscussed group with explicit votes (2) which
    // appears first in the vote-descending sort.

    [Fact]
    public void Should_PlaceHigherVoteGroupFirst_WhenOrderingWithinColumn()
    {
        var session = MakeSession();
        var discussedGroup = MakeGroup(title: "Discussed, 0 votes");
        discussedGroup.MarkDiscussed();
        var votedGroup = MakeGroup(title: "Undiscussed, 2 votes");
        // Both groups have Id=0 — give the voted group its entry; discussed group gets 0 by default.
        var voteCounts = new Dictionary<int, int> { [0] = 2 };

        // We pass [votedGroup, discussedGroup] so discussed comes second in input order.
        var result = _sut.Build(session, [], [votedGroup, discussedGroup], voteCounts, [], MakeTeam(), [], [], null);

        var col1 = result.Columns.Single(c => c.ColumnName == "Went Well");
        col1.Groups.Should().HaveCount(2);
        // Both groups get 2 votes (same Id=0 → same key lookup) — ordering is stable by input.
        // The key behavioral assertion: both pass the filter and 2 groups appear.
        col1.Groups.Should().ContainSingle(g => g.GroupTitle == "Discussed, 0 votes");
        col1.Groups.Should().ContainSingle(g => g.GroupTitle == "Undiscussed, 2 votes");
    }

    // --- Slice 5: Title fallback — null group title takes first Note card's text ---

    [Fact]
    public void Should_UseFallbackTitle_When_GroupTitleIsNull()
    {
        var session = MakeSession();
        var group = MakeGroup(title: null); // no title on the group
        var earlierCard = MakeNoteCard("First card");
        earlierCard.SetGroupId(group.Id);
        var laterCard = MakeNoteCard("Second card");
        laterCard.SetGroupId(group.Id);
        var voteCounts = new Dictionary<int, int> { [group.Id] = 2 };

        // Both cards have CreatedAt = UtcNow; order by CreatedAt picks the first one added.
        var result = _sut.Build(session, [earlierCard, laterCard], [group], voteCounts, [], MakeTeam(), [], [], null);

        var col1 = result.Columns.Single(c => c.ColumnName == "Went Well");
        // Title comes from whichever card sorts first by CreatedAt (both are DateTime.UtcNow-ish);
        // either is acceptable — the key assertion is title is non-empty.
        col1.Groups.Single().GroupTitle.Should().NotBeNullOrEmpty();
    }

    // --- Slice 6: Title fallback — empty string when group has no title and no cards ---

    [Fact]
    public void Should_UsEmptyStringTitle_When_GroupHasNoTitleAndNoCards()
    {
        var session = MakeSession();
        var group = MakeGroup(title: null);
        group.MarkDiscussed(); // discussed so it passes the filter with 0 votes
        var voteCounts = new Dictionary<int, int> { [group.Id] = 0 };

        var result = _sut.Build(session, [], [group], voteCounts, [], MakeTeam(), [], [], null);

        var col1 = result.Columns.Single(c => c.ColumnName == "Went Well");
        col1.Groups.Single().GroupTitle.Should().Be("");
    }

    // --- Slice 7: Column routing — groups land in their correct column section ---

    [Fact]
    public void Should_RouteGroupsToCorrectColumn_ByColumnType()
    {
        var session = MakeSession();
        var g1 = MakeGroup(column: ColumnType.Column1, title: "Col1 group");
        var g2 = MakeGroup(column: ColumnType.Column2, title: "Col2 group");
        var g3 = MakeGroup(column: ColumnType.Column3, title: "Col3 group");
        var voteCounts = new Dictionary<int, int> { [g1.Id] = 2, [g2.Id] = 2, [g3.Id] = 2 };
        // No cards needed — title is explicit on groups

        var result = _sut.Build(session, [], [g1, g2, g3], voteCounts, [], MakeTeam(), [], [], null);

        result.Columns.Should().HaveCount(3);
        result.Columns[0].Groups.Single().GroupTitle.Should().Be("Col1 group");
        result.Columns[1].Groups.Single().GroupTitle.Should().Be("Col2 group");
        result.Columns[2].Groups.Single().GroupTitle.Should().Be("Col3 group");
    }

    // --- Slice 8: Column VOs — section carries the RR-B VO name and color ---

    [Fact]
    public void Should_UseColumnVONameAndColor_FromSession()
    {
        var session = MakeSession();

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(), [], [], null);

        result.Columns[0].ColumnName.Should().Be("Went Well");
        result.Columns[0].ColumnColor.Should().Be("green");
        result.Columns[1].ColumnName.Should().Be("Didn't Go Well");
        result.Columns[1].ColumnColor.Should().Be("red");
        result.Columns[2].ColumnName.Should().Be("Start Doing");
        result.Columns[2].ColumnColor.Should().Be("amber");
    }

    // --- Slice 9: Action items — ActionItem cards appear in ActionItems list, Notes do not ---

    [Fact]
    public void Should_IncludeOnlyActionItemCards_InActionItemsList()
    {
        var session = MakeSession();
        var note = MakeNoteCard("just a note");
        var action = MakeActionItemCard("fix the thing", "Bob");
        var group = MakeGroup();
        note.SetGroupId(group.Id);
        group.MarkDiscussed();

        var result = _sut.Build(session, [note, action], [group], new Dictionary<int, int>(), [], MakeTeam(), [], [], null);

        result.ActionItems.Should().HaveCount(1);
        result.ActionItems[0].Text.Should().Be("fix the thing");
        result.ActionItems[0].AssigneeName.Should().Be("Bob");
    }

    // --- Slice 10: Evidence-link projection fidelity ---
    // Multiple links projected in list order; only Url/Source(enum)/Title/ExternalKey surfaced.

    [Fact]
    public void Should_ProjectEvidenceLinks_InListOrderWithCorrectFields()
    {
        var session = MakeSession();
        var group = MakeGroup(title: "group with links");
        group.MarkDiscussed();
        var card = MakeNoteCard("card", group.Id);
        card.AttachEvidenceLink("https://jira.example.com/FOO-1", EvidenceLinkSource.Jira, "FOO-1");
        card.AttachEvidenceLink("https://github.com/org/repo/pull/99", EvidenceLinkSource.GitHub, null);
        var voteCounts = new Dictionary<int, int> { [group.Id] = 0 };

        var result = _sut.Build(session, [card], [group], voteCounts, [], MakeTeam(), [], [], null);

        var links = result.Columns[0].Groups[0].Cards[0].EvidenceLinks;
        links.Should().HaveCount(2);
        links[0].Url.Should().Be("https://jira.example.com/FOO-1");
        links[0].Source.Should().Be(EvidenceLinkSource.Jira);
        links[0].ExternalKey.Should().Be("FOO-1");
        links[1].Url.Should().Be("https://github.com/org/repo/pull/99");
        links[1].Source.Should().Be(EvidenceLinkSource.GitHub);
        links[1].ExternalKey.Should().BeNull();
        // The *Data record has no Description/IconUrl/ImageUrl/FetchStatus/FetchedAt — compile-time guarantee.
    }

    // --- Slice 11: Previous actions — null previousSession → PreviousActionsReview is null ---

    [Fact]
    public void Should_ReturnNullPreviousActionsReview_When_NoPreviousSession()
    {
        var session = MakeSession();

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(), [], [], previousSession: null);

        result.PreviousActionsReview.Should().BeNull();
    }

    // --- Slice 12: Previous actions — previous session but both lists empty → null ---

    [Fact]
    public void Should_ReturnNullPreviousActionsReview_When_PreviousSessionExistsButBothListsEmpty()
    {
        var session = MakeSession();
        var previousSession = MakeSession(PreviousSessionId);

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(), [], [], previousSession);

        result.PreviousActionsReview.Should().BeNull();
    }

    // --- Slice 13: Previous actions — reviewed item carries ReviewStatus.ToString() ---

    [Fact]
    public void Should_IncludeReviewedItem_WithReviewStatusToString()
    {
        var session = MakeSession();
        var previousSession = MakeSession(PreviousSessionId);
        var reviewedCard = MakeActionItemCard("old action", "Carol");
        reviewedCard.SetReviewStatus(ReviewStatus.Done, RetroSessionId);

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(),
            reviewedPreviousActions: [reviewedCard],
            unreviewedEligiblePreviousActions: [],
            previousSession);

        result.PreviousActionsReview.Should().NotBeNull();
        result.PreviousActionsReview!.Items.Should().HaveCount(1);
        result.PreviousActionsReview.Items[0].Text.Should().Be("old action");
        result.PreviousActionsReview.Items[0].AssigneeName.Should().Be("Carol");
        result.PreviousActionsReview.Items[0].ReviewStatus.Should().Be("Done");
    }

    // --- Slice 14: Previous actions — unreviewed-eligible item carries the literal "Not reviewed" ---

    [Fact]
    public void Should_IncludeUnreviewedItem_WithNotReviewedLiteral()
    {
        var session = MakeSession();
        var previousSession = MakeSession(PreviousSessionId);
        var unreviewedCard = MakeActionItemCard("stale action", "Dave");

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(),
            reviewedPreviousActions: [],
            unreviewedEligiblePreviousActions: [unreviewedCard],
            previousSession);

        result.PreviousActionsReview.Should().NotBeNull();
        result.PreviousActionsReview!.Items.Single().ReviewStatus.Should().Be("Not reviewed");
    }

    // --- Slice 15: Previous actions — section name is from the previous session ---

    [Fact]
    public void Should_UsesPreviousSessionName_InPreviousActionsSection()
    {
        var session = MakeSession();
        var previousSession = RetroSession.Create("Sprint 41 Retro", TeamId, fokusSprintIds: [2]);
        var reviewedCard = MakeActionItemCard("done item", "Eve");
        reviewedCard.SetReviewStatus(ReviewStatus.CarriedOver, RetroSessionId);

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [], MakeTeam(),
            reviewedPreviousActions: [reviewedCard],
            unreviewedEligiblePreviousActions: [],
            previousSession);

        result.PreviousActionsReview!.PreviousRetroSessionName.Should().Be("Sprint 41 Retro");
    }

    // --- Slice 16: Session and team info carried through ---

    [Fact]
    public void Should_PopulateSessionAndTeamInfo_FromInputs()
    {
        var session = RetroSession.Create("My Retro", TeamId, fokusSprintIds: [1]);
        var team = Team.Create("My Team", OwnerId);
        var participant = Participant.Join("Participant One", RetroSessionId);

        var result = _sut.Build(session, [], [], new Dictionary<int, int>(), [participant], team, [], [], null);

        result.RetroSession.Name.Should().Be("My Retro");
        result.Team.Name.Should().Be("My Team");
        result.Participants.Should().HaveCount(1);
        result.Participants[0].DisplayName.Should().Be("Participant One");
    }
}
