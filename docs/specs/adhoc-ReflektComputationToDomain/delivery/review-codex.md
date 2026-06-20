# Codex Cross-Check — adhoc-ReflektComputationToDomain (RR-C)

**Verdict: GO**

All binding plan constraints are satisfied. No blocking or high-severity findings. Two low-severity observations documented below.

---

## Scope Checked

| File | Status |
|------|--------|
| `Reflekt.Domain/Reports/RetroReportData.cs` | New — reviewed |
| `Reflekt.Domain/Reports/RetroReportBuilder.cs` | New — reviewed |
| `Reflekt.API/Mappings/ReportMappings.cs` | Modified — reviewed |
| `Reflekt.API/Features/Reports/GetRetroReport/GetRetroReportEndpoint.cs` | Rewritten — reviewed |
| `Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs` | Rewritten — reviewed |
| `Reflekt.API/DependencyInjection.cs` | Modified — reviewed |
| `Reflekt.Domain.Tests/Reports/RetroReportBuilderTests.cs` | New — reviewed |
| `Reflekt.API/Features/Reports/ReportContracts.cs` | Unchanged — verified |

---

## Behavior Preservation

### Grouping and filter (`IsDiscussed || Votes > 1`)

Builder lines 45–56 reproduce the endpoint logic exactly: Note cards grouped by GroupId, ordered by CreatedAt; each group evaluated for `IsDiscussed || Votes > 1`; result ordered by votes descending. **Identical to the lifted original.**

### Title fallback (`g.Title ?? firstCard.Text ?? ""`)

Builder line 50: `g.Title ?? groupCards.FirstOrDefault()?.Text ?? ""`. `groupCards` is `cardsByGroup.GetValueOrDefault(g.Id, [])` ordered by `CreatedAt`. **Matches the original.**

### Column routing

Builder lines 59–64 call `BuildColumnSection` with `ColumnType.Column1/2/3` using the RR-B VOs (`session.Column1.Name`, `session.Column1.Color.Value`). **Correct.**

### Action-item projection

Builder lines 67–70: filter `CardType.ActionItem` from `cards`, project via `BuildActionItemData`. **Matches.**

### Evidence-link projection fidelity

`BuildEvidenceLinks` (line 112–113): projects `card.EvidenceLinks` in list order, carrying only `Url`, `Source` (enum), `Title`, `ExternalKey`. The five dropped fields (`Description`, `IconUrl`, `ImageUrl`, `FetchStatus`, `FetchedAt`) are not present on `ReportEvidenceLinkData` — compile-time guarantee, confirmed. **Matches the silent Mapster drop from before.**

### Previous-actions section short-circuit

Both endpoints: `previousSession` loaded first; if null, the two action-item queries are skipped (empty-list short-circuit). Builder returns null when `previousSession is null` OR when both lists are empty. **Matches the plan's binding note and the original `BuildPreviousActionsSectionAsync` behavior.**

### `"Not reviewed"` literal

`BuildPreviousActionsSection` line 132: `"Not reviewed"` literal for unreviewed-eligible items. `ReportPreviousActionData.ReviewStatus` is `string` — the plan's binding note is correctly implemented.

### `GetRetroReport` extra guard

`GetRetroReportEndpoint` line 28: `session.TeamId != req.TeamId` → NotFound. **Present and unchanged.** `GetParticipantReportEndpoint` correctly omits this guard (it has no `req.TeamId`).

### Auth preambles

Both endpoints: `participantId` claim → retroSessionId claim match (Unauthorized on mismatch); owner fallback → `team.OwnerId != userId` → Forbidden. The `GetRetroReport` team-ownership load uses `req.TeamId`; the `GetParticipantReport` team-ownership load uses `session.TeamId`. **Correct — each matches its original preamble.**

---

## ADR / Purity Checks

| Check | Result |
|-------|--------|
| `RetroReportBuilder` has no `DbContext` / repository / `CancellationToken` / Mapster | Confirmed — no such symbols in the file |
| `RetroReportData.cs` references no `Reflekt.API.*` or `*.Contracts` type | Confirmed — only `using Reflekt.Domain.Cards` (for `EvidenceLinkSource`) |
| `Reflekt.Domain.csproj` references only `Blocks.Domain` | Consistent with plan; no new project references introduced |
| Dead maps removed (`Card → ReportCardEntry`, `Card → ReportActionItemEntry`, `EvidenceLink → ReportEvidenceLinkEntry`) | Confirmed — `ReportMappings.cs` contains only the `RetroReportData → ReportResponse` tree |
| `ReportContracts.cs` unchanged | Confirmed — wire contract is byte-identical to pre-refactor |
| `RetroReportBuilder` registered `AddScoped` in `DependencyInjection.cs` | Confirmed — line 52, next to `GetSprintContextAssembler` |

---

## Mapster Map Completeness

`ReportMappings.cs` registers 10 configs:

- `RetroReportData → ReportResponse`
- `ReportRetroSessionInfoData → ReportRetroSessionInfo`
- `ReportTeamInfoData → ReportTeamInfo`
- `ReportParticipantInfoData → ReportParticipantInfo`
- `ReportColumnSectionData → ReportColumnSection`
- `ReportGroupEntryData → ReportGroupEntry`
- `ReportCardEntryData → ReportCardEntry`
- `ReportActionItemData → ReportActionItemEntry`
- `ReportEvidenceLinkData → ReportEvidenceLinkEntry` (with explicit `.Map(d => d.Source, s => s.Source.ToString())`)
- `ReportPreviousActionsData → ReportPreviousActionsSection`
- `ReportPreviousActionData → ReportPreviousActionEntry`

All nested record pairs are covered. The only non-name-identical field (`Source` enum → string) has an explicit map. All other fields are name-identical between domain records and wire records, so Mapster auto-maps. The `IReadOnlyList<T>` → `List<T>` collection type mismatch is within Mapster's standard conversion capability (it materializes to `List<T>` by default). **No map gaps.**

---

## Findings

### LOW-1 — Test slice 4 ordering limitation (not a defect; acknowledged in implementation.md)

**File:** `Reflekt.Domain.Tests/Reports/RetroReportBuilderTests.cs`, slice 4 (line 104–120)  
**Issue:** In-memory domain objects have `Id=0` — a `voteCountsByGroupId` dictionary keyed on `Id` cannot distinguish two groups, so the strict vote-descending order cannot be tested directly. The slice was rewritten to assert both groups appear (both get `Votes=2` from the same key lookup), rather than asserting relative position. The plan's ordering acceptance criterion is weakened here.  
**Impact:** The ordering behavior is correct by code inspection (`OrderByDescending(x => x.Votes)` at line 55 of the builder), but it is not covered by a test assertion. A regression in that sort line would not be caught by the suite.  
**Recommendation:** This is a known test-infrastructure limitation (documented in implementation.md). Acceptable as-is for a pure-unit suite. If integration tests against a seeded DB are added in a later pass, this is a natural candidate.

### LOW-2 — `Columns` and `ActionItems` typed as concrete `List<T>` in `ReportContracts.cs` vs `IReadOnlyList<T>` in domain records

**File:** `Reflekt.API/Features/Reports/ReportContracts.cs` (wire records); `RetroReportData.cs` (domain records)  
**Issue:** The wire records use `List<ReportParticipantInfo> Participants`, `List<ReportColumnSection> Columns`, `List<ReportActionItemEntry> ActionItems`, `List<ReportGroupEntry> Groups`, etc. The domain records use `IReadOnlyList<T>`. Mapster handles this conversion by default, but the type asymmetry is worth noting: if a future caller tries to use `.Adapt<ReportResponse>()` with a Mapster config that does not cover list covariance, it could fail silently. Current behavior is correct given Mapster's standard collection materialization.  
**Impact:** None at runtime with the current Mapster version. Not a defect — calling it out for completeness.  
**Recommendation:** No action needed. The existing Mapster configs and the `Adapt<ReportResponse>()` call site are already the pattern throughout this module.

---

## Summary

The implementation is a clean, faithful lift. All behavior-critical paths — grouping/filter, ordering, title fallback, column routing, action-item projection, evidence-link field set and order, previous-actions short-circuit, `"Not reviewed"` literal, and both auth preambles — are preserved exactly. The builder is pure (no DB/repo/HTTP), the domain records carry no API types, the Mapster maps are complete, the dead maps are removed, and the DI registration is correct. The test suite covers 16 slices across all behavioral branches except strict ordering (a documented infrastructure limitation). Build and tests are reported green.

**GO.**
