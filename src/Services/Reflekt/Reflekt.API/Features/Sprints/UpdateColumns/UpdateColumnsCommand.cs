using FluentValidation;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Sprints.UpdateColumns;

public sealed record UpdateColumnsCommand(
    int SprintId,
    string Column1Name,
    string Column1Color,
    string Column2Name,
    string Column2Color,
    string Column3Name,
    string Column3Color);

public sealed class UpdateColumnsCommandValidator : Validator<UpdateColumnsCommand>
{
    public UpdateColumnsCommandValidator()
    {
        RuleFor(x => x.Column1Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Column2Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Column3Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Column1Color).NotEmpty().Must(ColumnColor.IsValid).WithMessage("Column1Color must be one of the allowed palette values.");
        RuleFor(x => x.Column2Color).NotEmpty().Must(ColumnColor.IsValid).WithMessage("Column2Color must be one of the allowed palette values.");
        RuleFor(x => x.Column3Color).NotEmpty().Must(ColumnColor.IsValid).WithMessage("Column3Color must be one of the allowed palette values.");
    }
}
