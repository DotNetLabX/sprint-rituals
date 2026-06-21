# Sync Refactoring — Lessons

## Architect Lessons
- [APPLIED] When a refactoring plan requires amending a guardrail or convention, include the amendment as an explicit plan step with exact before/after text. This makes the convention change reviewable and prevents it from being treated as an afterthought.
- [APPLIED] Specifying line-number ranges for extraction targets (e.g., "replace the inner foreach loop, lines 64-99") gives both developer and reviewer a concrete anchor for verifying behavioral parity. Worth the upfront effort of reading the source.
- [TRACKED] Auto-approve gate (plan had 8 steps, no open questions) worked correctly here — no human approval round-trip was needed, and the pipeline completed in a single pass with zero fix cycles.

## Developer Lessons

- [APPLIED] When a focused operation service lives in a parent namespace (e.g., `Fokus.API.Features.Sync`) and is consumed by endpoints in child namespaces (e.g., `Fokus.API.Features.Sync.SyncSprints`), an explicit `using` is required even though the namespaces are related — C# does not auto-import parent namespaces.
- [TRACKED] `SprintMembership.FromJira` uses a defaulted positional parameter (`bool forcedNotCommitted = false`), so passing a variable positionally is valid — no need to use the named form, though named form (`forcedNotCommitted: value`) is used in the service for clarity.

## Reviewer Lessons

- [APPLIED] When reviewing a service extraction refactor, always retrieve the original endpoint code via `git show HEAD~1` rather than relying on the implementation.md description — subtle save-ordering and counter differences only show up in the actual before/after diff.
- [APPLIED] A removed `SaveChangesAsync` call is not automatically a data-loss risk: verify whether repositories share a DbContext (scoped DI) before flagging. In this codebase, all repositories inject the same scoped `FokusDbContext`, so any repository's `SaveChangesAsync` flushes all pending changes.
- [APPLIED] "No behavior change" in a refactoring plan refers to what data ends up persisted, not how many `SaveChangesAsync` calls are made. Batching saves within a service method is a valid improvement, not a deviation.

## Skill Gaps

- [APPLIED] **Missing skill:** "Extract response/DTO types from endpoint files into sibling command/query files" — suggested name: `extract-endpoint-types`. Coverage: pattern for splitting endpoint files into endpoint + command/query files, including which using statements to carry over. Reference files used: `GetJiraSprintsQuery.cs`, `SyncSprintsCommand.cs`.
- [APPLIED] **Missing skill:** "Extract shared endpoint logic into a focused operation service" — suggested name: `extract-operation-service`. Coverage: when to extract, naming convention (operation not entity), placement in feature area root, DI registration, result record pattern. Reference files used: `SprintIssueSyncService.cs` (new).
