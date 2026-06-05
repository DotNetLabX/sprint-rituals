namespace Xray.GraphQL;

/// <summary>
/// Singleton rate limiter for Xray Cloud API — Standard tier: 300 req/5-min window.
/// Extracted from GraphQLXrayClient so the window counter survives across scoped HTTP requests.
/// </summary>
public class XrayRateLimiter
{
    private const int RateLimitPerWindow = 290; // slightly under 300 to be safe
    private const int RateWindowSeconds = 300;  // 5 minutes

    private int _requestCount = 0;
    private DateTime _windowStart = DateTime.UtcNow;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task WaitForSlotAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            if ((now - _windowStart).TotalSeconds >= RateWindowSeconds)
            {
                _windowStart = now;
                _requestCount = 0;
            }

            if (_requestCount >= RateLimitPerWindow)
            {
                var waitMs = (int)(RateWindowSeconds * 1000 - (now - _windowStart).TotalMilliseconds);
                if (waitMs > 0)
                    await Task.Delay(waitMs, ct);
                _windowStart = DateTime.UtcNow;
                _requestCount = 0;
            }

            _requestCount++;
        }
        finally
        {
            _lock.Release();
        }
    }
}
