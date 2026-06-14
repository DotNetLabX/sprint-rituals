namespace Reflekt.API.Features.PreviousActions;

public sealed record PreviousActionItemResponse(
    int CardId,
    string Text,
    string? AssigneeName,
    string ReviewStatus,
    int PreviousSprintId,
    string PreviousSprintName);
