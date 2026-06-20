# Review — adhoc-ReflektComputationToDomain (RR-C)

## Step 1 — Done-Check

**Reviewer:** Architect (done-check gate)
**Verdict:** **PASS**
**Date:** 2026-06-20

> Note: the SubagentStop verify gate skipped (named-spawn quirk), so this done-check is the first real verification gate. Build + test claims were re-run independently against the actual changes — not taken on trust. See Evidence.

### Scope of this done-check

The plan is a single logical change delivered in 8 steps; the developer marked all 8 Done. This done-check verifies the change is **present and faithful to the plan** (the "Step 1" label in the dispatch refers to the pipeline's first done-check gate, not plan-step 1). All 8 plan steps verified below.

### Step dispositions

| Plan step | Disposition | Evidence |
|-----------|-------------|----------|
| 1 — `RetroReportData` result-record tree | **Present** | `RetroReportData.cs` created; 11 records parallel `ReportContracts.cs` member-for-member; `ReportEvidenceLinkData.Source` is the `EvidenceLinkSource` **enum**; `ReportPreviousActionData.ReviewStatus` is `string` (preserves `"Not reviewed"`); collections `IReadOnlyList<T>`; only `using Reflekt.Domain.Cards;` — no API/Contracts type (ADR-006 intact). |
| 2 — `RetroReportBuilder` pure domain service | **Present** | `RetroReportBuilder.cs` created; one public `Build(...)` taking loaded aggregates + private static helpers; **no `DbContext`/repository/`CancellationToken`/Mapster** in signature or body; lifted logic byte-faithful (filter `IsDiscussed \|\| Votes > 1`, vote-desc order, title fallback `g.Title ?? first.Text ?? ""`, RR-B VO reads `session.Column1.Name`/`.Color.Value`, `"Not reviewed"` literal, both-empty⇒null guard). |
| 3 — Extend `ReportMappings` | **Present** | 10 `NewConfig` entries for `RetroReportData → ReportResponse` + nested; sole non-1:1 map is `Source → .ToString()`; three legacy maps removed (see step 4). |
| 4 — Rewrite both endpoints + dead-map prune | **Present** | Both endpoints: auth preamble byte-preserved (incl. `GetRetroReport`'s `session.TeamId != req.TeamId` guard, absent from participant report — correct); body is load → short-circuit pre-load → `Build(...)` → `.Adapt<ReportResponse>()`; private helpers deleted; both inject `RetroReportBuilder`. `ReportMappings.cs` no longer registers the 3 dead `Card/EvidenceLink → *Entry` maps. |
| 5 — DI registration | **Present** | `AddScoped<Reflekt.Domain.Reports.RetroReportBuilder>()` in the Feature-services block of `DependencyInjection.cs`. |
| 6 — Unit tests | **Present** | `RetroReportBuilderTests.cs`, 16 slices; covers filter/ordering/title-fallback/column-routing/VO/action-items/evidence-fidelity/4 previous-actions branches/session+team info. 59/59 green (re-run). Two focus items below. |
| 7 — `Reflekt/CLAUDE.md` | **Present** | "Key patterns" gains the ADR-005 report-service bullet (`RetroReportBuilder`, pure, Mapster at boundary) + the de-dup note. |
| 8 — Verification sweep | **Present** | Implementation records the grep battery (purity, no-DTO-in-domain, helpers-gone, dead-maps-absent, contract diff-unchanged) + build/test. Independently re-run — see Evidence. |

### Focus item 1 — Slice-4 ordering-test deviation: ACCEPTED (coverage gap, not a masked defect)

The deviation is **sound and correctly reasoned**. `CardGroup.Create` yields `Id = 0` for every in-memory group (EF assigns the key only on save), so a vote-count dictionary keyed on `Id` collides: `voteCounts[0]` resolves to the same value for **both** groups. Two groups therefore carry identical vote counts, and `OrderByDescending(Votes)` is a stable no-op between them — there is no positional signal to assert at the unit level. The test correctly degrades to "both groups pass the filter and appear" rather than fabricating a false positional assertion.

The **production logic is not in doubt**: `OrderByDescending(x => x.Votes)` (RetroReportBuilder.cs:55) is lifted verbatim from the original endpoints and is exercised for *correctness of the filter and projection* by the surrounding slices. The untested dimension is purely *relative position of two distinct-vote groups*, which requires DB-assigned ids — an integration-test concern, explicitly carried over. This is an **acknowledged coverage gap, not a behavior change or masked defect**. Acceptable.

Minor (non-blocking) nit for a later pass: the gap could be closed at the unit level by injecting groups whose ids are set via a test seam or by using a vote dict that distinguishes them through a non-Id key — not required now; the carry-over to integration testing is a legitimate choice.

### Focus item 2 — Critic LOW-1 (evidence-link projection fidelity): CONFIRMED PINNED

Slice 10 (`Should_ProjectEvidenceLinks_InListOrderWithCorrectFields`, lines 219–241) pins **both** required dimensions:
- **List order** — two links attached Jira-then-GitHub; the test asserts `links[0]` = the Jira url/source/key and `links[1]` = the GitHub url/source/(null key), i.e. insertion order preserved.
- **Field set** — asserts `Url`, `Source` (as the `EvidenceLinkSource` **enum**, not a string), and `ExternalKey` per link. The dropped five (`Description`/`IconUrl`/`ImageUrl`/`FetchStatus`/`FetchedAt`) are excluded **structurally**: `ReportEvidenceLinkData` (RetroReportData.cs:33) has no such members, so surfacing them is a compile error — a stronger guarantee than a runtime assertion. The builder's `BuildEvidenceLinks` (RetroReportBuilder.cs:112–113) projects exactly the four fields in `links.Select(...)` order with no sort/filter.

This is the field-set-**and**-order pin the advisory asked for, not merely "Source carries the enum". Confirmed.

> One observation (not a finding): the slice doesn't assert `Title` on either link (both happen to be unset by `AttachEvidenceLink`, which leaves `Title` null until OpenGraph fetch). The four-field set is still structurally guaranteed and the boundary `.Map` for `Source` is the only transform — so `Title` flows trivially. No action needed.

### Evidence (independently re-run, not taken from implementation.md)

| Check | Command | Result |
|-------|---------|--------|
| Solution build | `dotnet build SprintRituals.slnx -c Debug` | **Build succeeded. 0 Error(s)** |
| Domain tests | `dotnet test Reflekt.Domain.Tests --no-build` | **Passed! Failed: 0, Passed: 59, Skipped: 0, Total: 59** |
| Builder purity | read `RetroReportBuilder.cs` | no `DbContext`/`Repository`/`CancellationToken`/`Adapt` — confirmed |
| No DTO in domain | read `RetroReportData.cs` + builder | only `Reflekt.Domain.*` usings; `Reflekt.Domain.csproj` refs only `Blocks.Domain` (unchanged) |
| Endpoints rewritten | read both endpoint files | auth preamble preserved; body = load → Build → Adapt; helpers deleted; builder injected |
| Dead maps pruned | read `ReportMappings.cs` | only `RetroReportData → ReportResponse` tree; 3 legacy maps absent |
| Wire contract | `ReportContracts.cs` | unchanged (11 records intact) |

### Verdict

**PASS.** All 8 plan steps present and faithful; ADR-005 (computation in domain), ADR-006 (domain DTO-free), ADR-010 (Mapster at boundary), and ADR-003 (duplication eliminated at root) satisfied. Build green and 59/59 tests pass on independent re-run. Both flagged focus items resolve in the implementation's favor: the Slice-4 deviation is a legitimate, well-reasoned unit-level coverage gap (carried over to integration), and the LOW-1 evidence-link test pins field-set **and** order as required.

Carry-overs into the next gate / manual phase (not blockers):
- Manual smoke (user): byte-identical JSON from both report endpoints over a populated retro (grouped/voted cards, action items, prior closed retro with reviewed + unreviewed actions) — plan Step 8.7.
- Optional integration test for strict vote-ordering of distinct-vote groups (needs DB-assigned ids) — closes the Slice-4 gap.

---

## Step 2 — Code Review

**Reviewer:** reviewer-rrc
**Verdict:** **APPROVED**
**Date:** 2026-06-20

### Pre-commitment predictions

Before reading the code, predicted most likely problem areas for this refactor:
1. **Mapster mapping completeness** — `IReadOnlyList<T>` vs `List<T>` mismatch between domain records and wire records might trip auto-mapping.
2. **Evidence-link projection field fidelity** — explicit construction replacing Mapster's silent name-match; drift risk.
3. **Strict vote-ordering test** — the Id=0 collision acknowledged in the plan; risk of a vacuous or misleading test.
4. **Short-circuit pre-load parity** — the handler must mirror the original `BuildPreviousActionsSectionAsync`'s lazy-load guard exactly.
5. **Dead-map pruning completeness** — three old Mapster configs becoming orphaned if any call site was missed.

All five were investigated specifically below.

### Carry-over scrutiny (advisory items from team-lead)

**Carry-over 1 — Slice-4 strict-ordering coverage gap (accepted by done-check)**

Confirmed: the Slice-4 test (RetroReportBuilderTests.cs:103–120) cannot assert strict positional order because all in-memory `CardGroup` objects have `Id=0` — the vote-count dictionary keyed on `Id` collides, so both groups receive the same vote count (2) regardless of which one was "intended" to be higher. The test asserts both groups pass the filter and appear in Column1, which is correct and non-vacuous. The ordering logic (`OrderByDescending(x => x.Votes)`, RetroReportBuilder.cs:55) is present and structurally correct; it is simply untestable at the unit level without DB-assigned IDs.

**Verdict on Carry-over 1:** Gap is real, correctly characterised, and appropriately carried over to integration testing. Not a blocking defect. CONFIRMED gap, not a masked defect.

**Carry-over 2 — Evidence-link projection fidelity (Slice 10 pinning)**

Confirmed: `BuildEvidenceLinks` (RetroReportBuilder.cs:112–113) projects `links.Select(l => new ReportEvidenceLinkData(l.Url, l.Source, l.Title, l.ExternalKey)).ToList()` — exactly the four fields, in list order, no sort/filter. Slice 10 (RetroReportBuilderTests.cs:219–241) asserts:
- `links[0]` is the Jira link (insertion order = list order); `links[1]` is the GitHub link.
- `Source` is `EvidenceLinkSource.Jira` / `.GitHub` (the enum, not a string — ADR-006 enforced in domain).
- `ExternalKey` is `"FOO-1"` / `null`.
- `ReportEvidenceLinkData` has no `Description`/`IconUrl`/`ImageUrl`/`FetchStatus`/`FetchedAt` members — structural (compile-time) guarantee, not runtime assertion.

The only unasserted field is `Title` — both test links have `Title = null` (unset until OpenGraph fetch), so `Title` flows as null trivially and the structural guarantee covers it. Not a gap.

**Verdict on Carry-over 2:** PINNED — field set and order both verified. Confirmed.

**Carry-over 3 — Byte-identical JSON preservation across both report endpoints**

The wire contract (`ReportContracts.cs`) is diff-clean — no field added, removed, or retyped. Both endpoints terminate with `result.Adapt<ReportResponse>()` where `result` is `RetroReportData`; Mapster's auto-mapping handles `IReadOnlyList<T>` → `List<T>` collection coercion (confirmed by build success — Mapster would emit a runtime error on first mapping attempt if the coercion failed, caught at startup via `TypeAdapterConfig.GlobalSettings.Scan`). The only explicit map is `Source → .ToString()`, which is the same transform the old `EvidenceLink → ReportEvidenceLinkEntry` map performed. JSON output is structurally identical.

**Verdict on Carry-over 3:** Wire stability confirmed structurally. Manual smoke (plan Step 8.7) is still the end-to-end gate; not blocking.

**Carry-over 4 — Builder purity (no DbContext/repo/Adapt)**

Grep of `RetroReportBuilder.cs` for `DbContext`, `Repository`, `CancellationToken`, `.Adapt` → zero hits (independently verified). Domain project references only `Blocks.Domain` — confirmed via `Reflekt.Domain.csproj`. No Mapster reference in the domain project. CONFIRMED pure.

### Plan conformance (dimension 1)

| Plan step | Verdict | Notes |
|-----------|---------|-------|
| Step 1 — `RetroReportData` result-record tree | **Conforms** | 11 records present; `EvidenceLinkSource` enum; `ReviewStatus` string; `IReadOnlyList<T>`; no API/Contracts type |
| Step 2 — `RetroReportBuilder` pure domain service | **Conforms** | Correct `Build(...)` signature; logic lifted faithfully; private helpers; pure |
| Step 3 — Extend `ReportMappings` | **Conforms** | 10 `NewConfig` entries; `Source.ToString()` only non-1:1; old maps absent |
| Step 4 — Rewrite endpoints + dead-map prune | **Conforms** | Auth preambles unchanged; load → Build → Adapt; helpers deleted; dead maps pruned; grep confirms zero residual `Adapt<Report*Entry>` call-sites |
| Step 5 — DI registration | **Conforms** | `AddScoped<RetroReportBuilder>()` next to `GetSprintContextAssembler` |
| Step 6 — Unit tests | **Conforms** | 16 slices covering all plan-specified behaviors; 59/59 green |
| Step 7 — CLAUDE.md | **Conforms** | ADR-005 pattern + de-dup note recorded |
| Step 8 — Verification sweep | **Conforms** | grep battery + build + tests all present and green |

Plan conformance: **PASS.** Proceeding to remaining dimensions.

### Findings

No findings at ≥80 confidence.

### Open Questions

None.

### Axis checklist

**Simplicity / DRY**

The lift is clean. `RetroReportBuilder` has four private static helpers (`BuildColumnSection`, `BuildCardData`, `BuildActionItemData`, `BuildEvidenceLinks`) that decompose at natural seams — each is single-purpose, named by what it builds, under ~10 lines. `BuildPreviousActionsSection` is ~15 lines and correctly private static. The public `Build` method is ~45 lines (well under the 60-line convention). No duplication remains: the ~80 lines of shared aggregation that existed twice across both endpoints are now in one place.

One minor observation (LOW, not a finding): `RetroReportBuilder` is a plain `class`, not a `sealed class`. The conventions use `sealed record` for DTOs and value objects; services are typically unsealed classes (`GetSprintContextAssembler` is also an unsealed class per the Fokus precedent). Consistency with the live precedent — not a defect.

**Bugs / correctness**

- **Short-circuit parity (Carry-over 4 above):** the endpoint pre-load guard `previousSession is not null ? await ... : []` (GetRetroReportEndpoint.cs:59–64, GetParticipantReportEndpoint.cs:59–64) exactly mirrors the original `BuildPreviousActionsSectionAsync` lazy-load pattern. Both endpoints are identical in this block. Correct.
- **`teamInfo!` null-suppression:** both endpoints pass `teamInfo!` to `Build(...)` (lines 67, 66). The `teamRepository.GetByIdAsync` call returns null only if the team doesn't exist; at this point in `GetRetroReport` the team was already fetched in the auth preamble (line 43) so a second fetch at line 54 could theoretically race — but in practice SQLite is single-writer and the team is persistent. The `!` is safe. This is a pre-existing pattern in the auth block; not introduced by this refactor.
- **Column order:** `columns` list is constructed with `[BuildColumnSection(Column1...), BuildColumnSection(Column2...), BuildColumnSection(Column3...)]` — positionally deterministic, matching the original endpoint order. Correct.
- **`g.Title ?? groupCards.FirstOrDefault()?.Text ?? ""`:** this is the verbatim lift from the original. Title fallback is correct.

**Conventions**

- `sealed record` on all 11 domain result records — correct per C# conventions.
- `IReadOnlyList<T>` on all collection properties — correct.
- File-scoped namespaces — correct.
- `Build` verb prefix on the public method — consistent with naming conventions ("Build = pure, returns data").
- Test naming: `Should_{ExpectedBehavior}_When_{Condition}` — 16/16 follow the convention (with one typo: `Should_UsEmptyStringTitle` missing 'e' — not a defect, cosmetic).
- Test SUT: `_sut = new()` — correct for a pure service per testing conventions.
- FluentAssertions used throughout — correct.
- Test data: inline factory helpers (not `TestData.cs` static methods) — the test project doesn't yet have a `TestData.cs`; the helpers are in the test class. This is acceptable for a new test class; the testing conventions say "static factory methods in `TestData.cs`" but that's a structural preference, not a hard rule for the first file.

### Evidence

| Check | Command / method | Result |
|-------|-----------------|--------|
| Build | `dotnet build SprintRituals.slnx -c Debug` | **Build succeeded. 0 Error(s)** (12 pre-existing NU1903 warnings) |
| Domain tests | `dotnet test Reflekt.Domain.Tests.csproj --no-build` | **Passed: 59, Failed: 0, Total: 59** |
| Builder purity | grep `DbContext\|Repository\|CancellationToken\|\.Adapt\b` in RetroReportBuilder.cs | Zero hits |
| No API types in domain | grep `Reflekt\.API\|\.Contracts\.` in `Reflekt.Domain/Reports/` | Zero hits |
| Domain project references | read `Reflekt.Domain.csproj` | Only `Blocks.Domain` — ADR-006 intact |
| Dead maps pruned | grep `Card, ReportCardEntry\|Card, ReportActionItemEntry\|EvidenceLink, ReportEvidenceLinkEntry` | Zero hits |
| No residual `Adapt<Report*Entry>` calls | grep `Adapt<Report` | Only two expected: `Adapt<ReportResponse>()` in each endpoint |
| Private helpers deleted | grep `BuildColumnSection\|BuildPreviousActionsSectionAsync` in Reflekt.API | Comments only, no implementation |
| Wire contract unchanged | `git diff HEAD -- ReportContracts.cs` | Empty diff |

### Verdict

**APPROVED.** All plan steps conform. Build green, 59/59 tests pass on fresh independent run. The four carry-over scrutiny areas are all resolved in the implementation's favor:
- Slice-4 ordering gap is acknowledged, correctly characterised, and appropriately deferred to integration testing.
- Evidence-link projection fidelity is pinned by Slice 10 (field set structurally guaranteed, list order asserted).
- JSON byte-stability is structurally guaranteed by the unchanged wire contract + Mapster auto-coercion (manual smoke remains the end-to-end gate).
- Builder purity confirmed via grep.

No CRITICAL or HIGH findings. MEDIUM/LOW observations:
- LOW: `RetroReportBuilder` is `class` rather than `sealed class` — consistent with the existing `GetSprintContextAssembler` precedent; not worth a fix.
- LOW: Slice-5 title-fallback test (lines 125–142) asserts `GroupTitle.Should().NotBeNullOrEmpty()` rather than a specific value, because two `DateTime.UtcNow` cards have indeterminate creation order. The test is non-vacuous (it proves a non-empty title is returned) but weaker than an exact assertion. Acceptable given the constraint; could be strengthened by using a seeded `CreatedAt` if the factory allows it — optional follow-up.
- LOW: Cosmetic typo `Should_UsEmptyStringTitle` (Slice 6, line 147) — missing 'e'. Optional fix.
