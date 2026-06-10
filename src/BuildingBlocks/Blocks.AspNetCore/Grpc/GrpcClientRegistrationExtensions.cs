using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;

namespace Blocks.AspNetCore.Grpc;

public static class GrpcClientRegistrationExtensions
{
    /// <summary>
    /// Registers a code-first gRPC client as a scoped service.
    /// Reads the target URL from <paramref name="grpcOptions"/> using the service key derived from
    /// <typeparamref name="TClient"/> (strips "Client"/"Service" suffix). Plain h2c channel — no TLS.
    /// </summary>
    public static IServiceCollection AddCodeFirstGrpcClient<TClient>(
        this IServiceCollection services,
        GrpcServicesOptions grpcOptions,
        string? serviceKey = null)
        where TClient : class
    {
        serviceKey ??= typeof(TClient).Name.Replace("Client", "").Replace("Service", "");

        if (string.IsNullOrWhiteSpace(serviceKey)
            || !grpcOptions.Services.TryGetValue(serviceKey, out var serviceSettings))
        {
            throw new InvalidOperationException($"Missing GrpcService config for: {typeof(TClient).Name}");
        }

        // Channel and typed client are registered as singleton — GrpcChannel is thread-safe and
        // designed to be shared. Creating a new channel per-request would leak sockets (L1).
        services.AddSingleton(_ =>
        {
            var channel = GrpcChannel.ForAddress(serviceSettings.Url);
            return channel.CreateGrpcService<TClient>();
        });

        return services;
    }
}
