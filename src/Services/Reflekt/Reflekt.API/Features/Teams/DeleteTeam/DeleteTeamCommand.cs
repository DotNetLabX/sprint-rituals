using FluentValidation;

namespace Reflekt.API.Features.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(int TeamId, string ConfirmationName);

public sealed class DeleteTeamCommandValidator : Validator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(x => x.ConfirmationName).NotEmpty();
    }
}
