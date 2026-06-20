namespace Reflekt.API.Features.Cards;

public sealed record EvidenceLinkResponse(
    int Id,
    int CardId,
    string Url,
    string Source,
    string? Title,
    string? Description,
    string? IconUrl,
    string? ImageUrl,
    string? ExternalKey,
    DateTime? FetchedAt,
    string FetchStatus);

public sealed record CardResponse(
    int Id,
    int RetroSessionId,
    int ParticipantId,
    string ColumnType,
    string CardType,
    string Text,
    string? AssigneeName,
    int? GroupId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int VoteCount = 0,
    List<EvidenceLinkResponse>? EvidenceLinks = null);

