using AwesomeAssertions;
using Reflekt.Domain.Participants;
using Xunit;

namespace Reflekt.Domain.Tests.Participants;

public class ParticipantJoinTests
{
    private const int RetroSessionId = 42;

    // --- Slice 1: authenticated join sets PersonId from the supplied id ---

    [Fact]
    public void Should_SetPersonId_When_PersonIdSupplied()
    {
        var participant = Participant.Join("Ana", RetroSessionId, personId: 7);

        participant.PersonId.Should().Be(7);
    }

    // --- Slice 2: guest join (no personId) leaves PersonId null (ADR-022) ---

    [Fact]
    public void Should_LeavePersonIdNull_When_NoPersonIdSupplied()
    {
        var participant = Participant.Join("Mihai", RetroSessionId);

        participant.PersonId.Should().BeNull();
    }
}
