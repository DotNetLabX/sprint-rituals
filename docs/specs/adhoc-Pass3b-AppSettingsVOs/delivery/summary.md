# Pass 3b — AppSettings VOs + Identity Encapsulation — Summary

**Slug:** adhoc-Pass3b-AppSettingsVOs · **ADR:** ADR-007 · **Status:** Done · **Branch:** main
**Team mode:** Standard + Codex · **Review mode:** critic (plan) + reviewer + Codex (implementation)

## What was delivered

Two independent arms of ADR-007 (aggregates encapsulate state; cohesive scalars become value objects):

1. **AppSettings VO grouping (Fokus).** Grouped 11 flat scalars on `AppSettings` into **4 new config VOs** — `BugRatioConfig` (AlertThreshold, ConsecutiveSprintCount, Target, DefaultSpPerBug), `XrayConfig` (Enabled, ClientId, ClientSecret), `CycleTimeConfig` (StartStage, EndStage), `SyncConfig` (SyncBackSprintCount, PlanningWindowDays). VOs are **domain-internal**; the wire (`GetSettingsResponse`, 5 Save commands, Vue client) stays **flat** (Q1=A), mapped flat↔grouped at the Settings endpoint boundary. EF persists the VOs as **flat columns via `ComplexProperty`** (Q4) + one migration (`20260607075717_GroupAppSettingsScalarsIntoVOs`). ~40 `settings.X` read sites rewritten to grouped paths across analytics (16 services + endpoints), gRPC, both sync services, Xray, DI seeding, and the 6 Settings endpoints. `BoardId`/`CompanyDomain`/status-lists stay top-level; the 4 existing health/quality VOs untouched.

2. **Identity encapsulation.** `Person` + `User` setters tightened to `private set`/`init`; `private` EF ctor on both so the factory + behavior methods (`CreateFromGoogle`, `CreateFromPerson`, `RecordLogin`, `ChangeRole`, …) are the sole mutation/creation path. No new behavior, no wire change.

**Out of scope (honored):** Fokus sync aggregates stay anemic (B3); wire contract unchanged; frontend untouched (`client/` diff empty).

## Pipeline gates

| Gate | Verdict | Notes |
|------|---------|-------|
| Architect plan + **critic** review | **APPROVE** (cycle 2) | Cycle 1 REQUEST CHANGES (1 HIGH: `SaveAsync` write funnel) → revised → all 4 findings RESOLVED |
| Developer implementation | Done | analyze+implement single pass; no blocking questions |
| Architect Step 1 **done check** | **PASS** | No step Missing; build green; 2 deviations ruled VALID |
| Reviewer Step 2 **code review** | **APPROVE** | 0 CRITICAL/HIGH; 1 MEDIUM (VO `class` vs `sealed record`, by design); fresh build green |
| **Codex** implementation review | **GO** (cycle 2) | Cycle 1 NO-GO: 1 BLOCKING (`User` implicit public ctor) + 1 WARNING (PlanningWindowDays default drift) → fixed → GO |

**Fix cycles:** 1/3 (Codex BLOCKING + WARNING). Codex caught the `User` ctor encapsulation gap that the nexus reviewer and done-check both missed — the Standard+Codex mode paid off.

## Deviations from plan (all ruled VALID)

- **A** — `required` dropped from `Person.FirstName/LastName/Email` (C# CS9032 forbids `required` + `private set`; invariant preserved by the `private Person()` ctor).
- **B** — explicit assignment instead of `IRegister`/`req.Adapt` for the 3 `Adapt` Save endpoints (within Step 4's stated latitude; avoids the same change-tracking hazard class as critic HIGH-1).

## Footprint

~46 files changed (~290 insertions / ~218 deletions) + 6 new files (4 VOs, migration + designer). Build: `dotnet build SprintRituals.slnx` green (0 errors; 4 pre-existing NU1903 warnings on Blocks.Contracts).

## Commits

- `4c2c98d` — feat(adhoc-Pass3b-AppSettingsVOs): add implementation plan
- implement — feat(adhoc-Pass3b-AppSettingsVOs): implement ADR-007 AppSettings config VOs + Identity encapsulation
- finalize — feat(adhoc-Pass3b-AppSettingsVOs): finalize after review

## Next

Pass **3c** (adhoc-Pass3c-ComputationToDomain) is now unblocked — migrate the 15 analytics services to Fokus.Domain calculators + endpoint-owned DTOs (ADR-005).

Slug: adhoc-Pass3b-AppSettingsVOs
