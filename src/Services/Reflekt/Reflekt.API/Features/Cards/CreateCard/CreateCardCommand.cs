using FluentValidation;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed record CreateCardCommand(int SprintId, string Text, string ColumnType, string CardType, string? AssigneeName)
    : ISprintAction
{
    public SprintAction ActionType =>
        CardType == "ActionItem" ? SprintAction.CreateActionItem : SprintAction.CreateNote;
}

public sealed class CreateCardCommandValidator : Validator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ColumnType).NotEmpty();
        RuleFor(x => x.CardType).NotEmpty();
        RuleFor(x => x.AssigneeName).MaximumLength(50).When(x => x.AssigneeName is not null);
    }
}
