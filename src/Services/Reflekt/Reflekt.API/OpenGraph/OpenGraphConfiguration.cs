using System.Threading.Channels;

namespace Reflekt.API.OpenGraph;

public static class OpenGraphConfiguration
{
    public static IServiceCollection AddReflektOpenGraph(this IServiceCollection services)
    {
        var channel = Channel.CreateBounded<FetchJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);

        services.AddHttpClient("opengraph", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        });

        services.AddHostedService<OpenGraphBackgroundService>();
        services.AddSingleton<OpenGraphFetcher>();

        return services;
    }
}
