# Re-Review: adhoc-Pass4-IdentityExtraction (Cycle 4 — FINAL)

## Verdict: GO / NO-GO
GO: the requested concurrency fixes are present, the revoke-vs-redeem race is closed at the specified touchpoints, and the seven prior fixes still hold at the cited locations.

## Fix Verification Summary
| Fix | Severity | Status | Evidence (file:line) |
|-----|----------|--------|----------------------|
| A: ConcurrencyToken in Accept() | HIGH | VERIFIED | `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/Invitation.cs:39` |
| B: ConcurrencyToken in Revoke() | HIGH | VERIFIED | `src/Services/Fokus/Fokus.Domain/Auth/Behaviors/Invitation.cs:48` |
| C: DbUpdateConcurrencyException in revoke endpoint | HIGH | VERIFIED | `src/Services/Fokus/Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationEndpoint.cs:23` |
| D: Token rotation removed from service layer | MED | VERIFIED | `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs:43` |
| FIX 1 Replay race redeem | HIGH | VERIFIED | `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs:49` |
| FIX 2 Inactive-user duplicate | HIGH | VERIFIED | `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:116` |
| FIX 3 RpcException caught | MED | VERIFIED | `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:146` |
| FIX 4 Pre-commit cross-module write | MED | VERIFIED | `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:196` |
| FIX 5 GetUserAsync dead seam | MED | VERIFIED | `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:263`; `src/Modules/Jira/Jira.Contracts/IJiraClient.cs:17` |
| FIX 6 Down migration safe | MED | VERIFIED | `src/Services/Identity/Identity.Persistence/Migrations/20260609194823_MakePersonEmailOptional.cs:43` |
| FIX 7 Channel-per-resolve | LOW | VERIFIED | `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs:30` |

## New Issues Found
None.

## Fix Detail

### Changes A+B
```csharp
public void Accept(DateTime now)
{
    if (Status != "Pending")
        throw new DomainException("Invitation is not in Pending status.");
    if (now > ExpiresAt)
        throw new DomainException("Invitation has expired.");

    Status = "Accepted";
    AcceptedAt = now;
    ConcurrencyToken = Guid.NewGuid();
}

public void Revoke()
{
    if (Status != "Pending")
        throw new DomainException("Only pending invitations can be revoked.");

    Status = "Revoked";
    ConcurrencyToken = Guid.NewGuid();
}
```

### Change C
```csharp
public override async Task HandleAsync(RevokeInvitationCommand req, CancellationToken ct)
{
    var invitation = await invitationRepository.GetByIdAsync(req.Id, ct);
    if (invitation is null)
    {
        await Send.NotFoundAsync(ct);
        return;
    }

    invitation.Revoke();
    try
    {
        await invitationRepository.SaveChangesAsync(ct);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
    {
        await Send.Results(Microsoft.AspNetCore.Http.Results.Conflict(), ct);
        return;
    }

    await Send.NoContentAsync(ct);
}
```

### Change D
```csharp
// All pre-checks passed — Accept will not throw.
invitation.Accept(DateTime.UtcNow);

try
{
    await invitationRepository.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    // Another request won the race — the invitation is now Accepted.
    return Fail(InvitationRedeemOutcome.Used);
}
```

### Prior Fixes Spot-Check
1. FIX 1 Replay race redeem: VERIFIED — `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs:49` catches `DbUpdateConcurrencyException` in `RedeemAsync`.
2. FIX 2 Inactive-user duplicate: VERIFIED — `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:116` resolves `existingUser` by `PersonId` before any new user creation path.
3. FIX 3 RpcException caught: VERIFIED — `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:146` catches `RpcException` in the invite-token branch.
4. FIX 4 Pre-commit cross-module write: VERIFIED — `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:196` starts the Identity upsert only after Fokus `SaveChangesAsync`.
5. FIX 5 GetUserAsync dead seam: VERIFIED — `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:263` calls `jiraClient.GetUserAsync(...)`, and `src/Modules/Jira/Jira.Contracts/IJiraClient.cs:17` still exposes the seam.
6. FIX 6 Down migration safe: VERIFIED — `src/Services/Identity/Identity.Persistence/Migrations/20260609194823_MakePersonEmailOptional.cs:43` throws `InvalidOperationException`, which is acceptable per the review criteria.
7. FIX 7 Channel-per-resolve: VERIFIED — `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs:30` registers the gRPC client as a singleton.
