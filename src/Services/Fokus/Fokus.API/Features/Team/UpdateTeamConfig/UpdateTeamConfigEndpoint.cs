namespace Fokus.API.Features.Team.UpdateTeamConfig;

public class UpdateTeamConfigRequest
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

public class UpdateTeamConfigValidator : Validator<UpdateTeamConfigRequest>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Developer", "Tech Lead", "Tester", "PO"
    };

    public UpdateTeamConfigValidator()
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

[HttpPut("/api/developers/{accountId}/team-config")]
[Tags("Team")]
[Authorize(Roles = "Admin")]
public class UpdateTeamConfigEndpoint(DeveloperRepository developerRepository)
    : Endpoint<UpdateTeamConfigRequest, UpdateTeamConfigResponse>
{
    public override async Task HandleAsync(UpdateTeamConfigRequest req, CancellationToken ct)
    {
        var developer = await developerRepository.UpdateTeamConfigAsync(
            req.AccountId,
            req.Role,
            req.DefaultCapacityPercent,
            req.SubTeam,
            req.SubTeamProvided ?? false,
            req.IsActive,
            ct);

        if (developer is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await developerRepository.SaveChangesAsync(ct);

        await SendOkAsync(new UpdateTeamConfigResponse
        {
            AccountId = developer.Id,
            DisplayName = developer.DisplayName,
            AvatarUrl = developer.AvatarUrl,
            Role = developer.Role,
            DefaultCapacityPercent = developer.DefaultCapacityPercent,
            SubTeam = developer.SubTeam,
            IsActive = developer.IsActive
        }, ct);
    }
}
