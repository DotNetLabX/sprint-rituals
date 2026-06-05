using System.Net.Http.Json;
using System.Text.Json;
using Blocks.Exceptions;

namespace Xray.GraphQL;

public class XrayBearerTokenManager
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Keyed by clientId to handle credential changes without restart
    private readonly Dictionary<string, (string Token, DateTime ExpiresAt)> _cache = new();

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(23);

    public XrayBearerTokenManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("XrayAuth");
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(clientId, out var cached) && DateTime.UtcNow < cached.ExpiresAt)
                return cached.Token;

            var token = await FetchTokenAsync(clientId, clientSecret, ct);
            _cache[clientId] = (token, DateTime.UtcNow.Add(TokenLifetime));
            return token;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateToken(string clientId)
    {
        _cache.Remove(clientId);
    }

    private async Task<string> FetchTokenAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        var request = new { client_id = clientId, client_secret = clientSecret };
        var response = await _httpClient.PostAsJsonAsync(
            "https://xray.cloud.getxray.app/api/v2/authenticate",
            request,
            ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedException("Xray authentication failed — check your Client ID and Secret.");

        if (!response.IsSuccessStatusCode)
            throw new BadGatewayException($"Xray authentication returned {(int)response.StatusCode}.");

        // Response body is a raw JWT string (quoted JSON string)
        var rawToken = await response.Content.ReadAsStringAsync(ct);
        // Trim surrounding quotes if present
        return rawToken.Trim('"');
    }
}
