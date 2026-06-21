# Xray Sync: Project-Wide TE Discovery — Questions

## Q1: knownTicketKeys source in BuildLinkMap
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 3 — Refactor XrayIssueSyncService for Xray-driven discovery
**File:** `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs`

**Context:** Step 3 says load `knownTicketKeys` from DB before calling `BuildLinkMap`, but `XrayIssueSyncService` has no `TicketRepository` in its constructor and `TicketRepository` has no `GetAllKeysAsync()` method.

**Question:** Inject `TicketRepository` into `XrayIssueSyncService` and load all ticket keys inline, or is there another approach?

### Answer
Inject `TicketRepository` into `XrayIssueSyncService` — correct. But do NOT load all ticket keys. Use the existing `TicketRepository.GetExistingKeysAsync(IEnumerable<string> keys)` method (line 29 of `TicketRepository.cs`), which takes candidate keys and returns only those that exist in the DB as a `HashSet<string>`.

Approach in `SyncTestExecutionsForProjectAsync`, before calling `BuildLinkMap`:
1. Collect all candidate ticket keys from the Xray response — every `OutwardIssueKey` and `InwardIssueKey` across all TE-level and test-case-level issuelinks.
2. Call `ticketRepository.GetExistingKeysAsync(candidateKeys, ct)` — single DB query, returns `HashSet<string>`.
3. Pass that HashSet as the `knownTicketKeys` parameter to `BuildLinkMap`.

Plan updated to reflect this. No new repository method needed.
