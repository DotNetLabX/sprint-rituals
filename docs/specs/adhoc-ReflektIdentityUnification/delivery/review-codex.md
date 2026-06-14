# Codex Cross-Check — RA2 ReflektIdentityUnification

## Verdict
NO-GO — the forward RA2 wiring is mostly consistent, but the migration is not operationally reversible on a non-empty database, and authenticated repeat-join now depends on a DB unique-index failure surfacing as a generic 500 instead of a controlled response.

## Critical Findings
None.

## Non-Critical Findings

**HIGH | Migration Down() — not operationally reversible**
`src/Services/Reflekt/Reflekt.Persistence/Migrations/20260614200041_RemoveReflektUserUnifyIdentity.cs` | `Down()` lines 54-105

`Down()` renames `PersonId` back to `UserId`, recreates an empty `Users` table, then immediately re-adds `FK_Participants_Users_UserId` and `FK_Teams_Users_OwnerId`. There is no step that recreates `User` rows or remaps `Teams.OwnerId` / `Participants.UserId` from Person ids back into valid `Users.Id` values. FK constraints will fail immediately after rollback on any non-empty database. The migration is schema-reversible only on an empty DB, not operationally reversible.

**MEDIUM | Authenticated repeat-join surfaces as unhandled 500**
`src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs` | `HandleAsync` lines 23-32

The join handler only checks display-name uniqueness before saving. `ParticipantConfiguration` now enforces a `(SprintId, PersonId)` unique index. `GlobalExceptionMiddleware` maps unknown exceptions to a generic 500. A second authenticated join by the same `person_id` with a different display name will hit the DB unique index and surface as an unhandled 500. `ParticipantRepository` has no `PersonId` lookup path; `GetCurrentParticipant` is guest-token-only.

## Confirmed Correct
- Identity adds the `person_id` cookie claim in `AuthConfiguration.cs:289-293`; Reflekt reads that exact same claim name in `ClaimsPrincipalExtensions.cs:12-25`. No claim-name disagreement.
- Owner-comparison repoints are consistent: `GetSprintSummaryEndpoint.cs:28-32` and `SprintHub.cs:198-222` both use Person id.
- `ActiveUserPreProcessor` scoping: the discriminator in `ActiveUserGateScope.cs:14-21` is Reflekt-exact and fail-safes on null-endpoint (null returns `false` = gate stays active for unknown endpoints, so Identity/Fokus gates are not disabled). Ordering is correct — the scope check happens before any `NameIdentifier` lookup.
- Gate-scope tests in `ActiveUserGateScopeTests.cs:14-34` explicitly cover: `Reflekt.API` exclusion, `Fokus.API` non-exclusion, `Identity.API` non-exclusion, and the null-endpoint case.
- No remaining live `ClaimTypes.NameIdentifier` reads in Reflekt code. The only surviving `UserId` symbol in `Reflekt.API` is `JoinSprintResponse.UserId`, mapped from `Participant.PersonId` in `ParticipantMappings.cs:11-13` — intentional wire-contract alias, not a value-space mismatch.
- Migration `Up()` is correct: drops both FKs before dropping `Users`, renames `Participants.UserId` to `PersonId`, renames the single-column index, recreates the filtered unique index on `(SprintId, PersonId)`.
- Reflekt's `User` aggregate/repo/EF config/`DbSet` are fully removed from live code. `ReflektDbContext`, `TeamConfiguration`, and `ParticipantConfiguration` model `OwnerId`/`PersonId` as plain cross-module ints with no `HasOne<User>()` FK.

## Notes
- No CRITICAL findings. The two non-critical findings do not block the RA2 feature — the HIGH issue is a rollback safety concern (Down() is academically schema-valid but not operationally safe on live data), and the MEDIUM is an edge-case UX gap on authenticated re-join (same person, different display name on second attempt).
- Findings are derived from static code inspection of the working-tree diff and referenced source files. Runtime behavior (DB rollback, endpoint execution) was not verified in the sandbox.
