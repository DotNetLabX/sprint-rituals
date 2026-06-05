using Identity.API.Features.Auth.GetUsers;

namespace Identity.API.Features.Auth.UpdateStatus;

[HttpPut("/api/users/{id}/status")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class UpdateStatusEndpoint(UserRepository userRepository, PersonRepository personRepository)
    : Endpoint<UpdateStatusRequest, UserResponse>
{
    public override async Task HandleAsync(UpdateStatusRequest req, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(req.Id, ct);
        if (user is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Last-Admin protection: prevent deactivating the last active Admin
        if (user.Role == "Admin" && !req.IsActive)
        {
            var adminCount = await userRepository.CountActiveAdminsAsync(ct);
            if (adminCount <= 1)
                throw new BadRequestException("Cannot deactivate the last active Admin.");
        }

        if (req.IsActive)
            user.Reactivate();
        else
            user.Deactivate();

        await userRepository.SaveChangesAsync(ct);

        var person = await personRepository.GetByIdAsync(user.PersonId, ct);

        await SendOkAsync(new UserResponse
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
