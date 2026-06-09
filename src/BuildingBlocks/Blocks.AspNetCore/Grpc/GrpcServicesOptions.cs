namespace Blocks.AspNetCore.Grpc;

/// <summary>
/// Configuration for gRPC code-first client dial targets.
/// Bind from the "Grpc" section: "Grpc:Services:{name}:Url".
/// </summary>
public class GrpcServicesOptions
{
    public Dictionary<string, GrpcServiceSettings> Services { get; init; } = [];
}

public class GrpcServiceSettings
{
    public string Url { get; init; } = default!;
}
