# adhoc-ReflektSprintOwnership (RA3) — Critic Review

## Mode: Plan Review (code-grounded; no spec — cross-referenced against ADR-016…022 + locked Q1–Q5)
## Verdict: REJECT

One CRITICAL: the route inventory is off by one. The plan's 6-rename / 24-keep split omits a 9th team-scoped route (`SetReviewStatus`), and Step 5's own acceptance grep is unsatisfiable as a result. Everything else is strong — the plan is otherwise rigorous, correctly grounded, and the two architect-flagged seams (migration FK, shared bus) are handled soundly. This is a localized fix, not a systemic rewrite.

## Pre-commitment Predictions (expected vs found)
1. **Migration FK cascade (Step 6)** — *Found: handled well.* Plan correctly pins child-column preservation via throwaway-DB introspection. BUT found a related gap: `Card.ReviewedInSprintId` (4th FK to the renamed table) is missing from the carve-out. → HIGH.
2. **MassTransit shared-bus (Step 4)** — *Found: handled well.* Plan forbids a 2nd bus, pre-sanctions escalation, leaves the (a)/(b) registration choice to the developer. Verified Fokus is the sole `AddMassTransit` with `AddConsumers(Fokus assembly)`. → MEDIUM note only.
3. **6/24 route split** — *Found: WRONG.* Actual team-scoped family is **9 routes, not 8**; `SetReviewStatus` is unaddressed. → CRITICAL.
4. **Homonym carve-out consistency** — *Found: mostly consistent* (`SprintHub`/`SprintRepository` collision and `GetSprintSummary` kept-route-renamed-response are explicitly pre-empted). One real gap: `Card.ReviewedInSprintId` (above).
5. **SprintSynced field set / publisher** — *Found: exactly correct.* Contract is the 5 fields the plan claims; single publish site confirmed.

## Cross-reference Matrix

### ADRs
| Source (ADR) | Plan Coverage | Status | Notes |
|---|---|---|---|
| ADR-016 (Identity owns Person; no gRPC client in RA3) | Step 3 "no gRPC to validate id"; Step 4 "no call back to Fokus" | COVERED | Consistent with RA2/ADR-016 timing. |
| ADR-017 (Fokus owns sprints; Reflekt references) | Goal #2, Step 2 link, Step 3 require ≥1 | COVERED | |
| ADR-018 (`RetroSession`; 1..N Fokus sprints) | Steps 1, 2, 3, 5; Rename Table | COVERED | |
| ADR-019 (`FokusSprintSnapshot`; `SprintSynced` consumer; one-directional) | Steps 2, 4 | COVERED | Snapshot = exactly 5 `SprintSynced` fields; verified vs contract. |
| ADR-020 (frontend) | Out of scope → RA5 | COVERED | Correctly deferred. |
| ADR-021 (waiting room) | Out of scope → RW | COVERED | |
| ADR-022 (no Person for guests) | Untouched (RA2 landed) | COVERED | Plan does not regress it. |

### Locked decisions Q1–Q5
| Decision | Plan Coverage | Status | Notes |
|---|---|---|---|
| Q1=b (rename aggregate + retro-state types; keep FK columns `SprintId`) | Rename Table + DO-NOT-TOUCH | **PARTIAL** | `Card.ReviewedInSprintId` FK column omitted from the kept-column list (HIGH below). |
| Q2 (rename HTTP routes/JSON; preserve SignalR strings) | Step 5 + DO-NOT-TOUCH | **PARTIAL** | `SetReviewStatus` team-scoped route unclassified (CRITICAL below). SignalR preservation is correct. |
| Q3=b (keep creation, require 1..N `FokusSprintIds`) | Step 3 | COVERED | Accept-unknown-id sub-decision resolved in-plan, consistent with Q4=a. |
| Q4=a (event-only/forward-only; no gRPC) | Steps 2, 4; OOS | COVERED | |
| Q5 (disposable dev DB) | Step 6 `Down()` schema-reversible | COVERED | |

## Findings

### [CRITICAL] Step 5 omits `SetReviewStatusEndpoint` — a 9th team-scoped route that is neither renamed nor carved out; its own acceptance grep becomes unsatisfiable
**Source:** Q2 (rename HTTP lifecycle routes); plan Step 5 rename table + DO-NOT-TOUCH carve-out + acceptance.
**Issue:** The plan asserts the team-scoped family is exactly the 6 rows in the Step 5 table (plus the renamed `CreateSprint`). The live grep returns **9 routes** under `/api/teams/{teamId}/sprints`:
- `CreateSprintEndpoint.cs`, `GetTeamSprintsEndpoint.cs`, `GetSprintEndpoint.cs`, `StartSprintEndpoint.cs`, `CloseSprintEndpoint.cs`, `AdvancePhaseEndpoint.cs` + `AdvancePhaseCheckEndpoint.cs`, `GetSprintReportEndpoint.cs` — **8 endpoints covered**.
- `SetReviewStatusEndpoint.cs:28` — `[HttpPatch("/api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}")]` — **the 9th, appears NOWHERE in the plan.** Not in the Step 5 rename table, not in the DO-NOT-TOUCH flat-route list (that list enumerates only `/api/sprints/{sprintId}/...` routes — `previous-actions` is named there as a flat route, but the team-scoped *write* path `SetReviewStatus` is distinct from the flat `GetPreviousActions` at `/api/sprints/{sprintId}/previous-actions`).

The plan's Step 5 acceptance is `grep -rn "\"/api/teams/{teamId}/sprints" src/Services/Reflekt` → **0 hits**. With `SetReviewStatus` untouched, this grep returns **1 hit** — the acceptance criterion can never pass while honoring the (implicit) carve-out, and the carve-out never mentions this route.

**Impact:** Developer is blocked at Step 5 with an unresolvable contradiction: (a) to pass the acceptance grep they must rename `SetReviewStatus`'s route — but the plan gives no target shape, forcing them to *invent* one, exactly what the byte-checkable rename table exists to prevent; or (b) leave it and fail their own done-check. Either way rework — and a silently mis-shaped route is a wire-contract defect RA5's frontend merge inherits. Same route-inventory miss-class the architect pre-flagged.
**Suggestion:** Add `SetReviewStatusEndpoint` to the Step 5 rename table with an explicit target route + the `SetReviewStatusCommand(int TeamId, int SprintId, …)` → `(int TeamId, int RetroSessionId, …)` rebind (note: the inner `command.SprintId` is used to call `card.SetReviewStatus(reviewStatus, command.SprintId)` at line 75 — that param flows to a `ReviewedInSprintId` FK, so verify the rename of the *route param* doesn't also rename the *FK-bound value's* semantics). Then correct the acceptance to reflect 9, not "the 6 table rows." Alternatively, if the user wants this route held for RA5, add it explicitly to DO-NOT-TOUCH and fix the acceptance grep to `→ 1 hit`.

### [HIGH] `Card.ReviewedInSprintId` — a 4th FK column to the renamed `Sprints` table — is missing from both the Q1 DO-NOT-TOUCH list and the Step 6 migration introspection
**Source:** Q1=b (keep child FK columns named `SprintId`); plan DO-NOT-TOUCH table + Step 6 acceptance.
**Issue:** The plan's DO-NOT-TOUCH list names only three child FK columns: `Participant.SprintId`, `Card.SprintId`, `CardGroup.SprintId`. But `CardConfiguration.cs` declares a **second** relationship to the retro table:
- `CardConfiguration.cs:18` — `builder.HasOne<Sprint>().WithMany().HasForeignKey(c => c.SprintId)` (covered)
- `CardConfiguration.cs:32` — `builder.HasOne<Sprint>().WithMany().HasForeignKey(c => c.ReviewedInSprintId).OnDelete(DeleteBehavior.SetNull)` — **a 4th FK to `Sprints`, NOT in the plan**
- `CardConfiguration.cs:33` — `builder.HasIndex(c => c.ReviewedInSprintId)` — its own index

When the principal renames `Sprint`→`RetroSession` / `Sprints`→`RetroSessions`, EF regenerates **two** Card→retro FK constraints (`FK_Cards_Sprints_SprintId` and `FK_Cards_Sprints_ReviewedInSprintId`) into `FK_Cards_RetroSessions_*`. The plan's Step 6 acceptance pins only `Participants.SprintId`/`Cards.SprintId`/`CardGroups.SprintId` columns + the `{SprintId,PersonId}` index as "unchanged" — it does not assert `Cards.ReviewedInSprintId` column survives unrenamed, nor that its index/constraint regenerate cleanly. `ReviewedInSprintId` is also a homonym (per Q1's intent it should stay column-named, a Reflekt-internal FK to the retro, deferred to RA5).
**Impact:** Two failure modes the plan's introspection wouldn't catch: (1) if EF's principal-rename cascade renames `ReviewedInSprintId`→`ReviewedInRetroSessionId` (a real risk given the `HasOne<RetroSession>` navigation), that violates Q1=b's "keep FK columns named `SprintId`-style"; (2) the SQLite table-rebuild for `Cards` must preserve this column + index, and the acceptance doesn't verify it. The plan's own RA2 lesson ("a parent rename can cascade column renames into children") applies here to a column it forgot to list.
**Suggestion:** Add `Card.ReviewedInSprintId` (column + `IX_Cards_ReviewedInSprintId` index + `FK_Cards_Sprints_ReviewedInSprintId` constraint) to the DO-NOT-TOUCH column list and to the Step 6 introspection acceptance (assert the column name unchanged and the index intact). Note it carries an additional homonym: `SetReviewStatusCommand` and `card.SetReviewStatus(reviewStatus, command.SprintId)` write this FK.

### [MEDIUM] Step 4 shared-bus: plan is sound but the `IConsumer` discovery mechanics deserve one more concrete pin
**Source:** Step 4 Build half; verified against `Fokus.API/DependencyInjection.cs:36-40`.
**Issue:** The plan correctly forbids a 2nd `AddMassTransit` and pre-sanctions architect escalation. Confirmed live: the only bus is `services.AddMassTransit(x => { x.AddConsumers(typeof(DependencyInjection).Assembly); … })` — `typeof(DependencyInjection)` is the **Fokus** type, so Reflekt's consumer assembly is genuinely not discovered today. The plan offers (a) add `x.AddConsumers(Reflekt assembly)` at the Fokus site or (b) a Reflekt-owned `AddReflektMessaging`. Option (a) places a Reflekt-assembly reference inside `Fokus.API.DependencyInjection` — creating a **Fokus→Reflekt project reference**, violating the CLAUDE.md "No cross-module project references" guardrail. Only option (b), composed at the Host, avoids that.
**Impact:** Low — the plan flags this as a developer decision with escalation, so it won't ship silently wrong. But option (a) as literally worded is guardrail-violating; the developer could waste a cycle discovering that.
**Suggestion:** Narrow Step 4's guidance: option (a) is only viable if the bus registration moves to the Host (where both assemblies are referenceable) — inside `Fokus.API` it breaks the no-cross-module-reference rule. Prefer (b) or a Host-composed single `AddMassTransit` that calls both modules' `AddConsumers`.

### [LOW] `GetSprintSummary` response rename note is correct; one adjacent symmetry worth stating
**Source:** Step 5 acceptance note on `RetroSessionSummaryResponse`.
**Issue:** The plan calls out that `GetSprintSummary` (flat route, kept) gets a renamed response `RetroSessionSummaryResponse`. Confirmed: `GetSprintSummaryEndpoint.cs:14` returns `SprintSummaryResponse`, built field-by-field (lines 35-52, not Mapster), so the rename is a type-name + constructor-call edit. The symmetric `GetParticipantReport`/`GetSprintReport` both return `ReportResponse` (not in the Sprint* rename set) — fine, but the plan never states `ReportResponse` and its `ReportSprintInfo` sub-record are intentionally NOT renamed.
**Impact:** Negligible — but `ReportSprintInfo` contains "Sprint" and a strict reader of the rename table might wonder.
**Suggestion:** One line confirming `Report*` types (`ReportResponse`, `ReportSprintInfo`) are out of the rename set (they describe the retro report, retained as-is in RA3).

## Gap Analysis
- **Missing requirement:** `SetReviewStatusEndpoint` route (CRITICAL) — the single most consequential omission; a team-scoped `/sprints` route the inventory missed.
- **Missing carve-out item:** `Card.ReviewedInSprintId` FK column/index/constraint (HIGH) — a homonym the DO-NOT-TOUCH list and migration introspection both skip.
- **Edge case — migration FK names:** With `SetReviewStatus`'s `command.SprintId` flowing into `ReviewedInSprintId` (CardConfiguration.cs:32, used at SetReviewStatusEndpoint.cs:75), a route-param rename on that endpoint risks accidentally implying a domain rename of the FK-bound value. The plan must state `ReviewedInSprintId` stays.
- **Backward impact:** None missed beyond the above. `SprintSynced` consumer is additive; no existing Reflekt feature is aware of `FokusSprintSnapshot` (correctly new). No Fokus-side change required (publisher already exists, verified).
- **Disposition completeness:** Out-of-scope section is thorough (RA4 gRPC, RA5 FK/flat-routes/SignalR, RW waiting room, Reflekt→Fokus events) — each with a forward reference. The two gaps above are the only requirements neither in-scope-addressed nor explicitly-deferred.

## Open Questions
- (None below LOW confidence — all findings carry direct file:line evidence.)
- For the user/architect to confirm at fix time: should `SetReviewStatus` rename now (Step 5) or defer to RA5 with the flat family? It is team-scoped (so it patterns with the renamed lifecycle routes) but operates on `previous-actions` keyed partly on the kept FK semantics — a genuine judgment call the plan should resolve, not the developer.

---

Reviewed: plan.md (against docs/architecture/decisions.md ADR-016…022, questions.md Q1–Q5, docs/backlog.md RA3, and live src/Services/Reflekt source). Critic agentId: a020dd87faa429d88.
