# Scope Change & Disruption — Review

## Reviewed By
`reviewer` (Sonnet agent — Step 2 code review, cycle 1 REQUEST CHANGES + cycle 1 re-review)

---

## Verdict: APPROVE

Both blocking findings from cycle 1 are resolved. No regressions introduced. No new issues found.

---

## Cycle 1 Findings — Resolution Status

### [RESOLVED] CRITICAL — Migration default `""` instead of `"[]"`

**Fix verified:**
- `Fokus.Persistence/Migrations/20260509080338_AddExcludedFromScopeStatuses.cs:18` — `defaultValue: "[]"` (was `""`)
- Line 20 — safety-net UPDATE SQL: `UPDATE AppSettings SET ExcludedFromScopeStatuses = '[]' WHERE ExcludedFromScopeStatuses = '';`

Both the column default and the safety net are present and correct. Any instance that had the old migration applied with `""` will be corrected on next `database update`.

### [RESOLVED] HIGH — "All Sprints" silently showed last 5

**Fix verified — full sentinel chain traced:**

1. `sprintsStore.ts:65`: `const last = selectedLast.value === null ? 0 : selectedLast.value` — "All" maps `null` → `0`
2. `sprintsStore.ts:66-70`: calls `getScopeChange(undefined, 0, ...)` — `0` is explicitly passed
3. `analytics.ts:32`: `if (last !== undefined) params.set('last', String(last))` — `0 !== undefined`, so `last=0` is appended to the URL
4. `GetScopeChangeEndpoint.cs:82`: `var last = req.Last ?? 5` — `req.Last` is `0` (provided), not null, so `last = 0`
5. `GetScopeChangeEndpoint.cs:85`: `last == 0 ? ascending : ascending.TakeLast(last).ToList()` — all sprints returned
6. `GetScopeChangeQuery.cs:19-22`: `GreaterThanOrEqualTo(0)` — validator allows `0`

The chain is correct at every link. "All Sprints" now fetches all closed sprints.

**Regression check — default `last=5` path:**
- No `last` param provided → `req.Last` is `null` → `req.Last ?? 5` = `5` → `last == 0` is false → `TakeLast(5)`. Unchanged. No regression.

---

## Pre-commitment Predictions (re-review)

| Predicted | Actual |
|-----------|--------|
| Migration `defaultValue` changed to `"[]"` | Confirmed at line 18 |
| Safety-net UPDATE SQL present | Confirmed at line 20 |
| Store sends `0` not `undefined` for "All" | Confirmed at line 65 |
| API function does not swallow `0` (falsy guard risk) | `if (last !== undefined)` — correct, `0` passes through |
| Validator updated to allow `>= 0` | Confirmed, `GreaterThanOrEqualTo(0)` |
| Backend branches on `last == 0` to skip `TakeLast` | Confirmed at line 85 |

All predictions confirmed. No surprises.

---

## Positive Observations (carried from cycle 1, still valid)

- **ScopeChangeService computation logic** remains correct — burnup, classification priority, delta computation, excluded-status filtering all verified in cycle 1.
- **AppSettingsRepository.SaveAsync** correctly assigns `ExcludedFromScopeStatuses` explicitly.
- **Classification priority order** matches spec business rules exactly.
- **Sentinel design is clean.** Using `0` as "all" is idiomatic for pagination patterns. The validator message at line 22 `"Last must be at least 0 (0 means all sprints)"` is self-documenting for API consumers.

---

## Gaps (unchanged from cycle 1 — no fixes required, noted for awareness)

- `GetActiveStatuses` with exactly 2 workflow stages returns both as "active" — low real-world impact.
- Sprint day number can theoretically be `0` if `AddedAt` is slightly before `StartDate` due to clock drift — unguarded but low risk.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build Fokus.API.csproj --no-incremental` | `Build succeeded. 0 Error(s). 3 Warning(s)` |
| Migration defaultValue | PASS | Read file | `defaultValue: "[]"` at line 18 |
| Safety-net SQL | PASS | Read file | `UPDATE AppSettings SET ... WHERE ExcludedFromScopeStatuses = '';` at line 20 |
| Store sentinel | PASS | Read file | `selectedLast.value === null ? 0 : selectedLast.value` at line 65 |
| API function falsy guard | PASS | Read file | `if (last !== undefined)` — `0` is sent |
| Validator allows 0 | PASS | Read file | `GreaterThanOrEqualTo(0)` at line 20 |
| Endpoint branches on 0 | PASS | Read file | `last == 0 ? ascending : ascending.TakeLast(last).ToList()` at line 85 |
| Default last=5 regression | PASS | Logic trace | `null ?? 5` = `5`, `5 == 0` false, `TakeLast(5)` — unchanged |
