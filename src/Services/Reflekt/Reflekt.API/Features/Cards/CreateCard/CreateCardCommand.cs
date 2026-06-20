using FluentValidation;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed record CreateCardCommand(int RetroSessionId, string Text, string ColumnType, string CardType, string? AssigneeName)
    : IRetroAction
{
    public RetroAction ActionType =>
        CardType == "ActionItem" ? RetroAction.CreateActionItem : RetroAction.CreateNote;
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

