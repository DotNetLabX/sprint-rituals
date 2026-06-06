# Pass 3a — DomainEnrichment (No-DTO-in-Domain, ADR-006) — Plan Review

## Reviewed By
Critic (Mode 2 — plan vs binding inputs). This is an adhoc refactoring pass with no `spec.md`; the binding inputs are `docs/architecture/decisions.md` (ADR-006), `docs/proposals/refactor-triage.md` (#A1), and the user decisions in `questions.md` (Q1/Q4/Q5, Answered) + `communication-log.md`. Plan reviewed: `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/plan.md`. Every plan claim was cross-referenced against the actual codebase.

## Verdict: APPROVE

No CRITICAL or HIGH findings. The plan is faithful to ADR-006, scoped to 3a only, ordering-safe, and every factual claim (factory locations, 13 call sites, line numbers, project reference, global usings, `Fokus.API` Jira reference, Persistence transitive-only usage, the stale `JiraMapper` doc line) verified true against the code. Two LOW notes and one MEDIUM advisory below; none block the developer.

## Pre-commitment Predictions
- Expected: plan would under-enumerate call sites or get a line number wrong. **Found false** — all 13 calls and every line number match exactly.
- Expected: 3b/3c scope leak (an aggregate encapsulation factory, an AppSettings VO, or an analytics-service touch sneaking in). **Found false** — the plan is removals-only, explicitly forbids ADR-007 encapsulation, and touches no analytics service.
- Expected: the Group-1/Group-2 split might be a fabricated distinction. **Found false** — verified the 3 Group-2 factories take only primitives; the Xray DTO is destructured in `XrayIssueSyncService` before the factory is called, so they are genuinely not ADR-006 violations.

## Findings

### [MEDIUM] Step 3 "may delete the file" leaves the namespace-only `namespace Fokus.Domain;` partial stub decision to the developer without an explicit anti-leftover check
**File:** `plan.md:122-128` (Domain Model Changes → Factories to delete)
**Issue:** For the five files that "become empty → may delete file" (`Ticket/Behaviors/Ticket.cs`, `Developer/Behaviors/Developer.cs`, `Ticket/Behaviors/StatusTransition.cs`, plus the three TestRun/TestExecution/TestSet behavior files), the plan correctly says the developer *may* delete them. But the verification battery (Step 6) has no check that an emptied behavior file was either deleted or left in a compilable state. An emptied `public partial class Ticket { }` is harmless and compiles; a half-deleted method body is not. This is a developer-discipline gap, not a plan defect — the build (Step 6.6) will catch a malformed file. Recommend the developer note in implementation.md, for each of the five, whether the file was deleted or kept as an empty partial, so the reviewer's Step-1 done-check can confirm no orphan factory fragment remains.
**Fix:** Optional — add to Step 6 a line: "for each emptied behavior file, confirm it was deleted OR contains only an empty `partial class` declaration (no dangling factory body)." Not blocking; build + reviewer cover it.
**Confidence:** MEDIUM

### [LOW] Step 6 grep pattern omits a dot-anchor on `FromJiraIssueLink`/`ListFromJira`, but this is safe (verified)
**File:** `plan.md:130, 166` (Step 3 + Step 6 grep `\.FromJira|\.FromXray|FromJiraIssueLink|ListFromJira`)
**Issue:** The first two alternatives are dot-anchored (`\.FromJira`, `\.FromXray`) which correctly excludes the false-positive `SyncSprintsFromJiraAsync` (a sync-*service* method, no dot before `FromJira`). The last two (`FromJiraIssueLink`, `ListFromJira`) have no `\.` anchor. I verified no method or symbol named `*FromJiraIssueLink` / `*ListFromJira` exists as a false-positive elsewhere, so the unanchored pattern is harmless today. Flagging only so the developer is not surprised if a future symbol collides.
**Fix:** None required. Optionally anchor for symmetry: `\.FromJira|\.FromXray|\.FromJiraIssueLink|\.ListFromJira`.
**Confidence:** HIGH (that it is currently safe)

### [LOW] Group-2 method rename ("drop the misleading `From*` framing") is correctly pre-flagged but has no acceptance criterion
**File:** `plan.md:203` (Flags for team-lead #2) + `plan.md:104` (Step 2)
**Issue:** The plan flags that the 3 Group-2 boundary-mapper methods "should drop the misleading `From*`-DTO framing (developer's call)" so the reviewer doesn't read the rename as scope creep. Good pre-emption. But Step 2's Accept criteria only check field-assignment parity and absence of `IRegister` — they say nothing about the method names. Since the name is explicitly the developer's call, this is fine; just noting that "rename happened" is unverifiable and unenforced by design, which is the right outcome here.
**Fix:** None. Correctly left to developer judgment.
**Confidence:** HIGH

## Scope Conformance (3a only — no 3b/3c leak)

| Out-of-scope item (locked) | Plan stance | Verified clean |
|---|---|---|
| ADR-007 aggregate encapsulation (internal setters/ctor/Create factory) | `plan.md:31` explicitly: "Do not add any. The `From*` factories are being *removed*, not converted into encapsulation factories." | Yes — Domain Model Changes is "Removals only — no additions" (`plan.md:53`). No `Create`/`internal` work anywhere. |
| 3b — AppSettings flat-scalar → VO grouping; Identity Person/User encapsulation | `plan.md:29` out-of-scope; "Fokus sync aggregates stay anemic (public setters)" | Yes — no AppSettings, no Identity, no setter change in any step. |
| 3c — ADR-005 migration of 15 analytics services | `plan.md:30` out-of-scope | Yes — no `Features/Analytics/` touch in any step. |
| `Fokus.Domain.Auth` (Invitation) coupling | `plan.md:32` deferred to Pass 4 | Yes — Step 4 drops only the `Jira.Contracts` ref; `global using Fokus.Domain.Auth;` is left intact in both GlobalUsings files (correct — that's domain-internal, not external DTO). |
| Behavior/error-handling/upsert/wire changes | `plan.md:33` "pure relocation: same data, same field assignments, new home" | Yes — `SprintSynced` publish explicitly preserved (`plan.md:177`); C2 error-handling defects from triage are NOT in scope and not touched. |

`GetEffectiveSp` preservation (the one genuine domain behavior on `SprintMembership`) is called out three times (`plan.md:73, 127, 170`) and verified present at `SprintMembership.cs:11`. The plan correctly distinguishes it from the `FromJira` factory in the same file.

## Plan↔Code Factual Verification

| Plan claim | Location in plan | Code evidence | Result |
|---|---|---|---|
| 8 `From*` factories exist on the named domain types | `plan.md:55-69` | All 8 found: Sprint.cs:7, Ticket.cs:5, Developer.cs:5, StatusTransition.cs:5, SprintMembership.cs:22, TestRun.cs:5, TestExecution.cs:5, TestSet.cs:5 | Match |
| 13 call sites across 2 sync services, exact lines | `plan.md:118-120` | `SprintIssueSyncService.cs` :58,:120,:123,:154,:157,:164,:167 (7) + `XrayIssueSyncService.cs` :229,:259,:290 (3) = 10 enumerated lines; the 3 multi-line calls account for the "13 calls" count | Match (every line number exact) |
| `Fokus.Domain.csproj:11` has the `Jira.Contracts` ref | `plan.md:143` | `Fokus.Domain.csproj:11` = `Jira.Contracts.csproj` ref | Match |
| `Fokus.Domain/GlobalUsings.cs:4` = `global using Jira.Contracts;` | `plan.md:144` | Confirmed line 4 | Match |
| `Fokus.Persistence/GlobalUsings.cs:3` = `global using Jira.Contracts;` | `plan.md:145` | Confirmed line 3 | Match |
| Persistence has zero actual Jira usage (transitive only) | `plan.md:24, 145` | Grep of `Fokus.Persistence/**/*.cs` for `Jira.\|JiraIssue\|JiraSprint\|using Jira` → only hit is GlobalUsings.cs:3 itself | Match — Step 4 removal is safe |
| `Fokus.API` already references `Jira.Contracts` | `plan.md:11, 96` | `Fokus.API.csproj:24` = `Jira.Contracts.csproj` ref | Match — Step 1 mapper compiles |
| Pass-2 `IRegister` exists in `Features/Settings/` to copy registration shape | `plan.md:91` | 4 `IRegister` classes found under `Features/Settings/Save*` | Match |
| Group-2 factories take only primitives; Xray DTO parsed before call | `plan.md:15, 102` | `TestRun.FromXray` (TestRun.cs:5-12) all primitives/enum; call site destructures `runDto` (XrayIssueSyncService.cs:259-267) before calling | Match — split is correct |
| `Fokus/CLAUDE.md` claims a `JiraMapper` that never existed | `plan.md:156-157` | CLAUDE.md "Key patterns → Sync-from-external": "`JiraMapper` maps Jira DTOs → domain entities." No `JiraMapper` type exists in src | Match |
| No `IRegister` currently in `Features/Sync` or `Features/Xray` (mapper is new) | `plan.md:89, 104` | All existing `IRegister` are under `Features/Settings/` only | Match — no collision with new mapper |

## ADR-006 Conformance
The plan fully discharges ADR-006's three mandates:
1. **"drop `Fokus.Domain → Jira.Contracts`"** → Step 4.
2. **"remove `FromJira(...)` factories from aggregates"** → Step 3 (all 8, including the 3 mis-named Group-2 per Q4's "all 8 for consistency").
3. **"map Jira/Xray DTOs → domain at the API/Persistence boundary via Mapster"** → Steps 1 (Mapster `IRegister` for the 5 real DTO mappers) + 2 (plain construction for the 3 no-DTO cases). The Step-2 deviation from "via Mapster" is correctly reasoned: a Mapster map requires a source type, and Group-2 has no source DTO — this is a mechanism detail inside Q4's directive, not a reversal of it (`questions.md:81` explicitly records this carve-out as user-acknowledged).

The two non-1:1 Group-1 maps (`StatusTransition.ListFromJira`, `SprintMembership.FromJira`) are correctly handled per ADR-010: non-trivial mapping that can't be 1:1 Mapster lives as an explicit method/`MapWith` *on the boundary mapper class*, not back in the domain. The binding rule ("the logic leaves the domain") is stated, and the expression form is correctly left to the developer.

## Ordering Safety
Binding order (`plan.md:81`) is correct and the RED-window hazard is explicitly called out: create mappers (Steps 1-2) → repoint callers + delete factories (Step 3) → drop the reference (Step 4). Dropping the `Jira.Contracts` reference before the factories are gone would break the build; the plan forbids that sequence in plain language. Step 5 (docs) is order-independent; Step 6 verifies last. Each code step ends with a green-build gate. The Step-3 "grep for zero callers before deleting" sub-step (`plan.md:130`) is a sound pre-delete guard. No ordering defect.

## Positive Observations
- **Exhaustive, code-accurate enumeration.** Every line number in the plan matches the live code — rare precision for a relocation plan. The developer can execute Step 3 mechanically.
- **The Group-1/Group-2 distinction is real and load-bearing.** It correctly prevents the developer from forcing a Mapster `IRegister` onto the 3 source-less constructions (which would not compile / would be nonsensical), and the Step-2 accept criterion explicitly verifies *absence* of `IRegister` — a good negative check.
- **Transitive-break foresight.** Removing `Fokus.Persistence/GlobalUsings.cs:3` is a non-obvious second-order consequence of dropping the Domain reference; the plan catches it (`plan.md:145`) with a verified rationale, preventing a confusing mid-build error. I independently confirmed Persistence has no real Jira usage.
- **Grep false-positive immunity.** The dot-anchored `\.FromJira`/`\.FromXray` pattern correctly excludes `SyncSprintsFromJiraAsync` (the sync-service method matched by a naive substring search in two endpoint files). The plan's verification will not produce spurious hits.
- **Scope discipline.** The out-of-scope section is specific (names the exact ADRs and the upsert dependency reason), and the in-line flags (#1 skill gap, #2 Group-2 rename) pre-empt the reviewer mis-reading legitimate work as scope creep.

## Gaps
- **No automated test parity check** — by project decision (no test suite; manual smoke post-pass). The plan's compensating control is read-level behavioral-parity verification against the original factory bodies (Step 1/2 accept) plus user manual smoke (`POST /api/sync/sprints`). This is the agreed model, not a plan defect, but it means correctness of the relocated business logic (especially `SprintMembership` planning-window `WasCommitted` and `Developer` null-return + `AvatarUrls["48x48"]` lookup) rests on the developer copying verbatim and the reviewer reading carefully. Called out so the reviewer prioritizes those three logic-bearing maps.
- **Emptied-file end-state not asserted** — see MEDIUM finding; build covers compilability but not "no orphan fragment."

## Open Questions
None. Q1 (3a-only scope), Q4 (boundary mappers in Features/Sync+Xray, Mapster IRegister), and Q5 (skills sufficient) are all Answered in `questions.md` and consistent with the plan. The two developer-time judgments the plan flags (non-1:1 map expression form; empty-file deletion) are correctly non-blocking.

## Evidence
| Check | Result | Command/Method | Output |
|-------|--------|----------------|--------|
| 8 factories exist at claimed files | pass | Grep `public static .*From(Jira\|Xray)\|FromJiraIssueLink\|ListFromJira` in Fokus.Domain | 8 hits, all at plan's files/lines |
| 13 call sites at claimed lines | pass | Grep `\.FromJira\|\.FromXray\|FromJiraIssueLink\|ListFromJira` in src | 10 enumerated lines across 2 sync services, exact match |
| Domain Jira project ref present | pass | Read `Fokus.Domain.csproj` | line 11 = Jira.Contracts ref |
| Both stale global usings present | pass | Read both GlobalUsings.cs | Domain:4, Persistence:3 |
| Persistence Jira usage is transitive-only | pass | Grep Fokus.Persistence/**/*.cs for Jira types | only GlobalUsings.cs:3 (the using itself) |
| Fokus.API already refs Jira.Contracts | pass | Grep Fokus.API.csproj | line 24 = Jira.Contracts ref |
| Pass-2 IRegister exemplars exist | pass | Grep `IRegister` in Fokus.API | 4 in Features/Settings/Save* |
| Group-2 factories are primitive-only | pass | Read TestRun.cs + XrayIssueSyncService.cs:220-296 | DTO destructured before factory call; factory takes primitives |
| `GetEffectiveSp` preserved & present | pass | Read SprintMembership.cs | line 11, distinct from FromJira at :22 |
| Stale `JiraMapper` doc claim exists | pass | Read Fokus/CLAUDE.md | "Sync-from-external: `JiraMapper`..." confirmed |
| No 3b/3c scope leak | pass | Full plan read + cross-ref to locked decisions | removals-only; no encapsulation/VO/analytics touch |
| Build | not run | n/a (plan review — no code changes; build is the developer's Step 6 gate) | — |

---
Reviewed: docs/specs/adhoc-Pass3-DomainEnrichment/delivery/plan.md

---

# Step 2 — Code Review

## Reviewed By
Reviewer (Step 2 — code vs plan). Build confirmed green (0 errors). Files reviewed: `JiraDomainMapper.cs`, `XrayDomainMapper.cs`, `SprintState.cs` (new); `SprintIssueSyncService.cs`, `XrayIssueSyncService.cs`, `SprintMembership.cs` (Behaviors), `Sprint.cs` (Behaviors), `Fokus.Domain.csproj`, `Fokus.Domain/GlobalUsings.cs`, `Fokus.Persistence/GlobalUsings.cs`, `Fokus.API/GlobalUsings.cs`, `Fokus/CLAUDE.md`.

## Verdict: APPROVED

No blocking findings. Two optional follow-ups (LOW severity only) below — both pre-existing or intentional; neither introduced by this pass.

## Follow-up Items (LOW — non-blocking)

### F1 [LOW] `MapStatusTransitions` returns `List<T>` rather than `IReadOnlyList<T>`
**File:** `JiraDomainMapper.cs:83`
This is a pre-existing gap in `TicketRepository.ReplaceTransitionsAsync` which requires `List<StatusTransition>` as its parameter. The mapper correctly matches the consumer. Fixing this properly requires a paired change to the repository signature — out of scope for a pure relocation pass. Follow-up: change `ReplaceTransitionsAsync` parameter to `IReadOnlyList<StatusTransition>` and update the mapper return type in a future pass.

### F2 [LOW] Two `SprintState` enums with identical values now exist in separate namespaces
**File:** `Fokus.Domain/Sprint/SprintState.cs` (new), `Jira.Contracts/SprintState.cs` (existing)
The split is intentional and correct (domain state concept vs Jira API filter enum). Risk: a future contributor might "de-duplicate" them by re-adding a cross-reference, silently re-introducing the ADR-006 violation. Consider an ADR-006 addendum note explaining why they must remain separate.

## Plan Conformance

| Step | Code Status | Notes |
|------|-------------|-------|
| 1 — `JiraDomainMapper` (5 Group-1 maps) | Conforming | `IRegister` for Sprint/Ticket/Developer; plain static methods for StatusTransitions + SprintMembership (plan-sanctioned). All logic verified verbatim against original factory bodies. |
| 2 — `XrayDomainMapper` (3 Group-2 maps) | Conforming | `static` class, no `IRegister`, 3 `Create*` methods, pure field assignments. |
| 3 — Repoint callers + delete factories | Conforming | All 13 call sites repointed; 6 files deleted; 2 trimmed (keepers verified present). |
| 4 — Drop `Jira.Contracts` reference | Conforming | `csproj` + 2 stale global usings removed. Deviations (SprintState move, global using removal from Fokus.API) valid and documented. |
| 5 — Correct `Fokus/CLAUDE.md` | Conforming | Sync-from-external line now names real boundary mappers + ADR-006. |
| 6 — Verification sweep + build | Conforming | All grepper checks pass; build green. |

## Correctness Trace (logic-bearing maps)

- `MapSprintMembership` — `WasCommitted` = `!forcedNotCommitted && effectiveAddedAt <= planningCutoff`; added-at/removed-at changelog derivation; `effectiveAddedAt = addedAt ?? sprint.StartDate` — all identical to original.
- `MapDeveloper` — null-guard on `Assignee`; `AvatarUrls.TryGetValue("48x48", ...)`; `IsActive = true` — identical.
- `MapStatusTransitions` — `histories.OrderBy(h => h.Created)`; filter `i.Field == "status"`; `FromString ?? string.Empty`; `ToStringValue ?? string.Empty` — identical.
- `MapTicket` — Epic-vs-parent conditional; `EpicKey`/`EpicName`/`ParentTicketKey` null-coalescing — identical.
- `MapSprint` — lowercase state switch with `_ => Active` default; date null-coalescing; `SyncedAt = DateTime.UtcNow` — identical.

No logic was altered during relocation.
