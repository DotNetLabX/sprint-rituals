using Identity.API.Features.Auth.GetUsers;

namespace Identity.API.Features.Auth.UpdateRole;

[HttpPut("/api/users/{id}/role")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class UpdateRoleEndpoint(UserRepository userRepository, PersonRepository personRepository)
    : Endpoint<UpdateRoleCommand, UserResponse>
{
    public override async Task HandleAsync(UpdateRoleCommand req, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Last-Admin protection: prevent demoting the last active Admin
        if (user.Role == "Admin" && req.Role == "Manager")
        {
            var adminCount = await userRepository.CountActiveAdminsAsync(ct);
            if (adminCount <= 1)
                throw new BadRequestException("Cannot demote the last active Admin.");
        }

        user.ChangeRole(req.Role);
        await userRepository.SaveChangesAsync(ct);

        var person = await personRepository.GetByIdAsync(user.PersonId, ct);

        await Send.OkAsync(new UserResponse
        {
            Id = user.Id,
            Email = person?.Email ?? string.Empty,
            DisplayName = person?.FullName ?? string.Empty,
            AvatarUrl = person?.PictureUrl,
            Role = user.Role,
            LastLoginAt = user.LastLogin,
            IsActive = user.IsActive
        }, ct);
    }
}
