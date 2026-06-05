using System.Security.Claims;

namespace Identity.API.Features.Auth.GetMe;

[HttpGet("/api/auth/me")]
[Tags("Auth")]
public class GetMeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override Task HandleAsync(CancellationToken ct)
    {
        var user = HttpContext.User;

        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var id = int.TryParse(idStr, out var parsed) ? parsed : 0;

        return Send.OkAsync(new MeResponse
        {
            Id = id,
            Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            AvatarUrl = user.FindFirstValue("avatar_url"),
            Role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        }, ct);
    }
}
