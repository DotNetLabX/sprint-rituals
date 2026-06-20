using Reflekt.API.Features.Cards;

namespace Reflekt.API.Features.CardGroups;

public sealed record CardGroupCardEntry(int Id, string Text, DateTime CreatedAt, List<EvidenceLinkResponse> EvidenceLinks);

public sealed record CardGroupResponse(int Id, int RetroSessionId, string ColumnType, string? Title, List<CardGroupCardEntry> Cards, int VoteCount, bool IsDiscussed, bool IsFocused);

