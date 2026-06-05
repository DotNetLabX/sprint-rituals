namespace Xray.Contracts;

public interface IXrayClient
{
    Task<string> AuthenticateAsync(string clientId, string clientSecret, CancellationToken ct);
    Task<XrayTestExecutionResult> GetTestExecutionsAsync(string bearerToken, List<string> issueKeys, CancellationToken ct);

    /// <summary>
    /// Fetches all Test Executions in the project using JQL <c>project = {projectKey}</c>.
    /// Each TE is returned with its test runs, test cases, and issue links populated.
    /// </summary>
    Task<XrayTestExecutionResult> GetAllProjectTestExecutionsAsync(string bearerToken, string projectKey, CancellationToken ct);

    /// <summary>
    /// Sends a minimal GraphQL query to verify the bearer token has data-access permissions.
    /// Throws <see cref="Blocks.Exceptions.UnauthorizedException"/> or
    /// <see cref="Blocks.Exceptions.BadGatewayException"/> on failure.
    /// </summary>
    Task ProbeAsync(string bearerToken, CancellationToken ct);
}
