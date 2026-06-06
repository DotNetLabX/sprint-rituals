namespace Fokus.API.Features.Team.UpdateTeamConfig;

public class UpdateTeamConfigCommand
{
    public string AccountId { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int? DefaultCapacityPercent { get; set; }
    public string? SubTeam { get; set; }
    public bool? SubTeamProvided { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateTeamConfigResponse
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public int DefaultCapacityPercent { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateTeamConfigCommandValidator : Validator<UpdateTeamConfigCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Developer", "Tech Lead", "Tester", "PO"
    };

    public UpdateTeamConfigCommandValidator()
    {
        When(r => r.DefaultCapacityPercent.HasValue, () =>
        {
            RuleFor(r => r.DefaultCapacityPercent!.Value)
                .InclusiveBetween(0, 100)
                .WithMessage("Default capacity percent must be between 0 and 100.");
        });

        When(r => r.Role is not null, () =>
        {
            RuleFor(r => r.Role!)
                .NotEmpty()
                .WithMessage("Role must not be empty.")
                .Must(role => AllowedRoles.Contains(role))
                .WithMessage("Role must be one of: Developer, Tech Lead, Tester, PO.");
        });
    }
}
