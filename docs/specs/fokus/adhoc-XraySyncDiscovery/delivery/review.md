# Xray Sync: Project-Wide TE Discovery — Review

## Reviewed By
reviewer (Sonnet agent)

## Verdict: APPROVE

## Cycle 2 Re-Review

Both HIGH findings from cycle 1 are resolved. No new issues found.

### Finding 1 (HIGH) — KB not updated: RESOLVED

`docs/kb/xray.md` Sync Flow section has been fully rewritten:
- Now describes two distinct sync paths (dedicated endpoint vs piggyback)
- Step-by-step flow for `POST /api/xray/sync` correctly documents `GetAllProjectTestExecutionsAsync`, `GetExistingKeysAsync`, `BuildLinkMap`, and separate TestSet path
- Link Type Detection section updated: source is now "Xray GraphQL response" not "sprint ticket issuelinks", with the three-row table clearly separating test-case-level and TE-level sources
- TestSet exception documented: still Jira-sourced, correctly called out
- Key files list updated to current method names (`SyncTestExecutionsForProjectAsync`, `SyncTestSetsFromIssuesAsync`, `BuildLinkMap`)

### Finding 2 (HIGH) — JQL injection risk: RESOLVED

`GraphQLXrayClient.cs:79-87` — `ProjectKeyFormat` compiled regex (`^[A-Z0-9_]+$`) added as a static field. Guard applied at the top of `GetAllProjectTestExecutionsAsync` before any interpolation. Exception type is `BadRequestException`, consistent with the endpoint-level null guard. Regex is `Compiled` — correct for a hot path.

---

## Findings

None.

---

## Positive Observations

All cycle 1 positives carry forward. Additionally:

- `ProjectKeyFormat` declared as `static readonly` compiled regex — correct placement and performance posture.
- KB update is thorough: not just the Sync Flow section but also Link Type Detection and Key Files list — all three stale sections addressed.
- `BadRequestException` used for format violation (line 87) — consistent with the endpoint's null check (line 50 of `SyncXrayEndpoint.cs`), giving callers uniform 400 behavior for all project key issues.

---

## Gaps (carry-forward, pre-existing)

- Test-case pagination silent truncation: no warning logged when `tests.total > 100` for a given TE. Pre-existing limit, not introduced here.
- `SyncTestSetsFromIssuesAsync` internal warnings list not surfaced in the response. Pre-existing behavior, not introduced here.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| CS compiler errors (Xray.GraphQL) | PASS | `dotnet build Xray.GraphQL.csproj --no-restore \| grep " error CS"` | (no output — zero CS errors) |
| CS compiler errors (Fokus.API) | PASS | `dotnet build Fokus.API.csproj --no-restore \| grep " error CS"` | (no output — zero CS errors) |
| ProjectKeyFormat regex present | PASS | Read `GraphQLXrayClient.cs:79` | `private static readonly Regex ProjectKeyFormat = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);` |
| Guard applied before interpolation | PASS | Read `GraphQLXrayClient.cs:86-87` | `if (!ProjectKeyFormat.IsMatch(projectKey)) throw new BadRequestException(...)` |
| KB Sync Flow updated | PASS | Read `docs/kb/xray.md:19-46` | Two-path architecture documented; Xray-driven TE discovery described correctly |
| KB Link Type Detection updated | PASS | Read `docs/kb/xray.md:48-62` | Source column added; Xray GraphQL stated as source; TestSet Jira exception documented |
| Dead code absent (all removed methods) | PASS | grep across src/ | Zero matches for all removed method names |
