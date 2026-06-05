using Identity.API.Features.Auth.GetUsers;

namespace Identity.API.Features.Auth.GetUsers;

[HttpGet("/api/users")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class GetUsersEndpoint(UserRepository userRepository)
    : EndpointWithoutRequest<List<UserResponse>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var pairs = await userRepository.GetAllWithPersonAsync(ct);

        var result = pairs.Select(p => new UserResponse
        {
            Id = p.User.Id,
            Email = p.Person.Email,
            DisplayName = p.Person.FullName,
            AvatarUrl = p.Person.PictureUrl,
            Role = p.User.Role,
            LastLoginAt = p.User.LastLogin,
            IsActive = p.User.IsActive
        }).ToList();

        await Send.OkAsync(result, ct);
    }
}
