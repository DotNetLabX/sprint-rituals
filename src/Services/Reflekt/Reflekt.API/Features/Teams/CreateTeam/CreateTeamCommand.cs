using FluentValidation;

namespace Reflekt.API.Features.Teams.CreateTeam;

public sealed record CreateTeamCommand(string Name);
public sealed record CreateTeamResponse(int Id, string Name, DateTime CreatedAt);

public sealed class CreateTeamCommandValidator : Validator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
