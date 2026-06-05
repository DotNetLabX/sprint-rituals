using Xray.Contracts;

namespace Fokus.API.Features.Xray.TestConnection;

public class TestConnectionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

[HttpPost("/api/xray/test-connection")]
[Tags("Xray")]
[Authorize(Roles = "Admin")]
public class TestConnectionEndpoint(AppSettingsRepository settingsRepository, IXrayClient xrayClient)
    : EndpointWithoutRequest<TestConnectionResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);

        if (!settings.XrayEnabled || string.IsNullOrEmpty(settings.XrayClientId) || string.IsNullOrEmpty(settings.XrayClientSecret))
            throw new BadRequestException("Xray is not enabled or credentials are not configured.");

        // AuthenticateAsync throws UnauthorizedException (401) or BadGatewayException (502) on failure
        var token = await xrayClient.AuthenticateAsync(settings.XrayClientId, settings.XrayClientSecret, ct);

        // ProbeAsync sends a minimal GraphQL query (limit 1, no JQL) to verify data-access permissions
        await xrayClient.ProbeAsync(token, ct);

        await SendOkAsync(new TestConnectionResponse
        {
            Success = true,
            Message = "Connected — Xray access verified."
        }, ct);
    }
}
