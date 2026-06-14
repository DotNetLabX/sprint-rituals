using FluentValidation;

namespace Reflekt.API.Features.Cards.EditCard;

public sealed record EditCardCommand(int SprintId, int CardId, string Text, string? AssigneeName);

public sealed class EditCardCommandValidator : Validator<EditCardCommand>
{
    public EditCardCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AssigneeName).MaximumLength(50).When(x => x.AssigneeName is not null);
    }
}
