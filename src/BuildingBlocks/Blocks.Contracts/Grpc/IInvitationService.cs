using System.ServiceModel;
using Blocks.Contracts.Grpc.DataContracts;

namespace Blocks.Contracts.Grpc;

/// <summary>
/// gRPC contract for synchronous invitation redemption.
/// Fokus = server (hosts the Invitation domain). Identity = client (calls from the Google auth callback).
/// One atomic call: Fokus validates usability, enforces case-insensitive email match,
/// calls Invitation.Accept(now) on success, and returns the outcome.
/// </summary>
[ServiceContract]
public interface IInvitationService
{
    [OperationContract]
    Task<RedeemInvitationResponse> RedeemAsync(RedeemInvitationRequest request, CancellationToken ct = default);
}
