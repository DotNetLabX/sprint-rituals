using FluentValidation;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Sprints.CreateSprint;

public sealed record CreateSprintCommand(
    int TeamId,
    string Name,
    int? VotesPerParticipant,
    string? Column1Name,
    string? Column1Color,
    string? Column2Name,
    string? Column2Color,
    string? Column3Name,
    string? Column3Color,
    int? WritingTimerDefault,
    int? VotingTimerDefault,
    int? DiscussingTimerDefault);

public sealed class CreateSprintCommandValidator : Validator<CreateSprintCommand>
{
    public CreateSprintCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VotesPerParticipant).GreaterThanOrEqualTo(1).When(x => x.VotesPerParticipant.HasValue);
        RuleFor(x => x.Column1Name).MaximumLength(30).NotEmpty().When(x => x.Column1Name is not null);
        RuleFor(x => x.Column2Name).MaximumLength(30).NotEmpty().When(x => x.Column2Name is not null);
        RuleFor(x => x.Column3Name).MaximumLength(30).NotEmpty().When(x => x.Column3Name is not null);
        RuleFor(x => x.Column1Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column1Color must be one of the allowed palette values.");
        RuleFor(x => x.Column2Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column2Color must be one of the allowed palette values.");
        RuleFor(x => x.Column3Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column3Color must be one of the allowed palette values.");
        RuleFor(x => x.WritingTimerDefault).InclusiveBetween(1, 60).When(x => x.WritingTimerDefault.HasValue);
        RuleFor(x => x.VotingTimerDefault).InclusiveBetween(1, 60).When(x => x.VotingTimerDefault.HasValue);
        RuleFor(x => x.DiscussingTimerDefault).InclusiveBetween(1, 60).When(x => x.DiscussingTimerDefault.HasValue);
    }
}
