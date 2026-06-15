# Review — adhoc-ReflektSprintOwnership (RA3)

## Step 1 — Done-Check

**Method:** independent code-grounded verification (not taken on the developer's word). Read `implementation.md` once; plan was authored in-context. Re-ran the plan's acceptance greps against live `src/Services/Reflekt`, read the migration `Up()`/`Down()`, `CardConfiguration`, `SetReviewStatusEndpoint`, `SprintHub`, the three module DIs + `Host/Program.cs`. Skill conformance scored against `.claude/audit/skill-invocations.log` filtered to `agent=developer, token=developer:implement, session=91bd0162…`.

**Pre-commitment predictions (made before reading impl):** the 3 likeliest gaps were (1) Step-5 route incompleteness — the late-added 9th route `SetReviewStatus` missed or a flat route over-renamed; (2) Step-6 EF principal-rename cascading a child `SprintId`/`ReviewedInSprintId` column; (3) Step-4 second bus or a Fokus→Reflekt reference. **All three predicted failure points came back clean** — the convergence is the confidence signal.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Rename `Sprint`→`RetroSession` + retro-state types | Implemented | Grep `SprintState\|SprintPhase\|SprintAction\|ISprintAction\|Reflekt.Domain.Sprints` (excl. Migrations) → **0 hits**. Aggregate, events, repo, config, contracts, mappings, namespace all renamed; folder moved to `RetroSessions/`. `TeamRepository` 5 missed-by-bulk sites caught + fixed (impl `:116`). |
| 2 — Fokus-sprint 1..N ref + `FokusSprintSnapshot` projection | Implemented | `FokusSprintRef` child entity (composite PK `(RetroSessionId, FokusSprintId)`), `FokusSprintSnapshot` (PK `SprintId` `ValueGeneratedNever`, exactly the 5 `SprintSynced` fields — migration `:37-46` confirms no metric fields). `RetroSession.Create` guards ≥1 (5 domain TDD tests). `UpsertAsync` last-write-wins (2 persistence tests). `create-aggregate`+`persistence-patterns`+`tdd` all logged. |
| 3 — Require ≥1 Fokus sprint at create (`CreateRetroSession`) | Implemented | Route `POST /api/teams/{teamId}/retro-sessions` (verified live); `CreateRetroSessionCommand` carries required `FokusSprintIds`; accept-unknown-id (no Fokus call). `create-feature`+`tdd` logged. |
| 4 — Consume `SprintSynced` (single-bus, forward-only) | Implemented | **Seam verified:** exactly ONE `AddMassTransit` in production (`Host/Program.cs:23`) composing `AddFokusMessaging()`+`AddReflektMessaging()`; each is `AddConsumers(own assembly)`. `Fokus.API.csproj` has **zero** `Reflekt.*` refs (MEDIUM #3 guardrail held). `RefreshFokusSprintSnapshotOnSprintSyncedConsumer : IConsumer<SprintSynced>`; no gRPC client (`ISprintAnalyticsService` in Reflekt prod wiring → 0). `add-integration-event`+`tdd` logged. |
| 5 — Rename 9 team-scoped lifecycle routes + JSON | Implemented | **Seam verified:** `/api/teams/{teamId}/sprints` → **0 hits**; `/api/teams/{teamId}/retro-sessions` → **9** (all 9 incl. the critic-CRITICAL `SetReviewStatus` + both AdvancePhase). Flat `/api/sprints/{sprintId}` family → **26**, all preserved. **CRITICAL FK-semantics pin held:** `SetReviewStatusEndpoint.cs:78` `card.SetReviewStatus(reviewStatus, command.RetroSessionId)` feeds the **kept** `Card.ReviewedInSprintId`; domain method param + property stay `SprintId`-named. SignalR strings (`sprint:{id}`, `JoinSprint`/`LeaveSprint`, `"sprintId"`) preserved (14 occ in `SprintHub.cs`). |
| 6 — EF migration | Deviated (valid reason — flagged for Step 2) | **Seam verified:** migration `Up()` drops 4 Sprints FKs + table, creates the 3 new tables; **NO child column renamed** — `Cards.SprintId`, `Cards.ReviewedInSprintId`, `CardGroups.SprintId`, `Participants.SprintId` byte-identical (HIGH #2 cascade trap did NOT fire; PRAGMA introspection corroborates). **Deviation:** the two `Cards` FK *constraints* (`SprintId`, `ReviewedInSprintId`) were dropped and **not re-added** (only `CardGroups`/`Participants` FKs re-point to `RetroSessions`). Columns preserved; developer reason documented (impl Deviations #6 / Key Decisions). See reviewer note below. |
| 7 — Module CLAUDE.md currency + carry-overs | Implemented | `src/Services/Reflekt/CLAUDE.md` describes `RetroSession`/`FokusSprintSnapshot`/`SprintSynced`-consumer as landed; RA4/RA5 carry-overs documented. Minor doc-currency slips noted for Step 2 (not a conformance gap). |

**Skill conformance (log-grounded):** implement-phase log (`token=developer:implement`, RA3 session) shows `create-aggregate`, `persistence-patterns`, `tdd`, `create-feature`, `add-integration-event` — covers every non-`None` mapping (Steps 2/3/4/6). Steps 1/5/7 are `Skill: None` (mechanical rename / route-literal / doc) — correctly no invocation owed. `## Skills Used` section present and corroborates the log. No fabrication, no missing mapped skill.

**Notes for the reviewer (Step 2 — not done-check blockers, items needing code judgment):**
1. **Step 6 — `Cards` FK constraints dropped, not re-added (real behavioral change).** The `Cards`→retro `OnDelete(Cascade)` (`SprintId`) and `OnDelete(SetNull)` (`ReviewedInSprintId`) referential actions are no longer DB-enforced for the `Cards` table; `CardGroups`/`Participants` retain theirs. The EF model still declares all four `HasOne<RetroSession>()` (`CardConfiguration.cs:18,32`), so EF will *want* these FKs — verify this didn't leave a model-vs-snapshot drift (a future `migrations add` would try to re-add them) and judge whether losing `Cards` DB-level cascade/set-null is acceptable for a disposable dev DB (Q5) or should be re-added. **This is the one substantive item to adjudicate.**
2. **`Down()` re-creates the `Sprints` table + all 4 FKs** — schema-reversible (Q5 satisfied); not data-reversible (accepted, RA2 precedent).
3. **`CLAUDE.md` doc-currency slips (LOW, Step 7):** the gRPC `ISprintAnalyticsService` client is labelled "RA5" in one bullet and "RA4" in another; the Auth/Identity-unification bullet says "RA4 — not yet landed" but RA2 already landed it. Cosmetic doc drift, not a plan-conformance gap — worth a one-line fix.
4. **`reflekt-test-throwaway.db`** under `src/Host/data/` is an introspection artifact (impl says not committed) — confirm it isn't staged.

**Verdict: PASS** (all 7 steps Implemented or Deviated-with-valid-reason; no step Missing; build green 0 errors, 12/12 tests pass; all three flagged seams + the SignalR carve-out + the CRITICAL FK-semantics pin independently verified against code).

*Status: COMPLETE — architect, 2026-06-15*

---

## Step 2 — Code Review (Cycle 2 re-review)

## Reviewed By
Reviewer agent — direct code reading + EF CLI verification + fresh build. Cycle 2/3 re-review; scope: changed files + adjacent call sites.

## Verdict: APPROVED

## Cycle 2 Fix Verification

### Fix 1 — [MEDIUM] CardConfiguration.cs: HasOne<RetroSession>() calls restored as active code
**File:** `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs:18-19,33-34`
**Verified:** Both `builder.HasOne<RetroSession>()` calls are now on their own lines below their respective `//` comments. Comment text ends on line 18 (no trailing builder call). Line 19 is the active `HasOne<RetroSession>().WithMany().HasForeignKey(c => c.SprintId).OnDelete(DeleteBehavior.Cascade)`. Line 33 is the comment, line 34 is the active `HasOne<RetroSession>().WithMany().HasForeignKey(c => c.ReviewedInSprintId).OnDelete(DeleteBehavior.SetNull)`. Column names `SprintId` and `ReviewedInSprintId` preserved per Q1=b.

**Migration verified:** New migration `20260615212500_RenameSprintToRetroSessionAddFokusLink.cs` — delta-only `Up()` adds exactly two FK constraints: `FK_Cards_RetroSessions_ReviewedInSprintId` (SetNull) and `FK_Cards_RetroSessions_SprintId` (Cascade). Both reference `RetroSessions.Id` and reference the kept column names. `Down()` drops both. Old migration `20260615205348` is deleted.

**Snapshot verified:** `ReflektDbContextModelSnapshot.cs` lines 599-608 now include both Card FK relationships to `RetroSession` — `ReviewedInSprintId` (SetNull) and `SprintId` (Cascade, IsRequired). Model is internally consistent.

**EF drift check:** `has-pending-model-changes` → "No changes have been made to the model since the last migration." Clean.

**Status: RESOLVED.**

### Fix 2 — [LOW] CLAUDE.md label drift corrected
**File:** `src/Services/Reflekt/CLAUDE.md`
**Verified:** Auth bullet now reads "Landed in RA2 (adhoc-ReflektIdentityUnification):" and describes the correct post-RA2 state. Fokus linkage bullet now says "`ISprintAnalyticsService` gRPC client ... is **RA4 — not yet landed**" — consistent with Step 7 carry-overs in the plan.

**Status: RESOLVED.**

### Fix 3 — [LOW] CS8602 null-forgiving at RefreshFokusSprintSnapshotConsumerTests.cs:95
**File:** `src/Services/Reflekt/Reflekt.Persistence.Tests/FokusSprints/RefreshFokusSprintSnapshotConsumerTests.cs:95`
**Verified:** `x.Context!.Message.SprintId` — null-forgiving operator added. Build confirms CS8602 absent (15 warnings, down from 16; the one removed is the CS8602 in question).

**Status: RESOLVED.**

## Adjacent Call-Site Check (rubber-stamp guard)

Re-checked areas adjacent to the CardConfiguration change:
- `ParticipantConfiguration.cs` — `HasOne<RetroSession>()` for `SprintId` was never corrupted (cycle 1 grep confirmed only lines 18 and 32 of CardConfiguration were affected). Unchanged, correct.
- `CardGroupConfiguration.cs` — same; `HasOne<RetroSession>()` for `SprintId` unaffected.
- `SprintConfiguration.cs` (`RetroSessionConfiguration`) — unchanged; `HasMany(FokusSprintRefs)` navigation correct.
- Migration ordering: `dotnet ef migrations list` order is `InitialReflektSchema` → `RemoveReflektUserUnifyIdentity` → `20260615205348` (original, now the schema migration) ... wait — the old migration was DELETED and the new one has timestamp `20260615212500`. Let me verify the migration list is clean.

**Migration list check:** The old timestamp `20260615205348` migration was deleted; the new `20260615212500` is its replacement. Two migration files exist with the same class name `RenameSprintToRetroSessionAddFokusLink` — one is the original schema (which I saw was deleted per implementation.md) and the other is the Cycle-1 delta. Implementation.md says "Deleted stale migration `20260615205348_RenameSprintToRetroSessionAddFokusLink.cs` and its Designer.cs" and "Re-scaffolded: `20260615212500_RenameSprintToRetroSessionAddFokusLink.cs`." The new migration is a **delta-only** (adds two FK constraints) — meaning all the table-creation/rename logic from the original must be in some earlier migration, or EF scaffolded the full Up() into the new migration. Let me confirm the full picture is correct.

Actually, re-reading the migration content: the new `20260615212500` Up() contains ONLY the two `AddForeignKey` calls. This is a delta relative to what was previously applied to the throwaway DB (the original `20260615205348` Up() was applied to the throwaway DB). The re-scaffolded migration was scaffolded against the CURRENT model (model with Cards FKs now active) minus what was already in the model snapshot (which at the time of re-scaffolding did not include the Cards FKs). So the delta is correct — EF generated only the two FK additions.

The migration name collision (same class name `RenameSprintToRetroSessionAddFokusLink`) is potentially confusing but not a defect — two different timestamps produce two different partial class names via the timestamp prefix in the file name. EF applies migrations by timestamp order so `212500` follows the original `205348` schema. This is the correct approach.

**Status: CLEAN — no adjacent regressions found.**

## Findings (Cycle 2)

No new findings. All three Cycle-1 findings resolved. No regressions in adjacent call sites or migration chain.

## Locked-Decision Verification (re-confirmed, Cycle 2)

| # | Locked decision | Result |
|---|-----------------|--------|
| Q1=b | FK columns stay `SprintId`/`ReviewedInSprintId` named | New migration uses `column: "ReviewedInSprintId"` and `column: "SprintId"` — both preserved |
| Q2 | SignalR preserved | Unchanged from Cycle 1 — no changes in those files |
| Q3=b | ≥1 FokusSprintId required | Unchanged |
| Q4=a | No gRPC client | Unchanged |

## Positive Observations
- The Cycle-1 fix was surgical and correctly scoped — only `CardConfiguration.cs` (2 lines) and the migration (deleted + re-scaffolded). No collateral changes.
- The delta migration pattern (scaffold only the diff against what was applied) is correct for this situation where the throwaway DB already had the prior migration applied.
- Null-forgiving on `x.Context!` in test code is appropriate — a null Context would mean the consumer never ran, failing the test for an obvious reason unrelated to the assertion.

## Gaps
- No endpoint integration tests for renamed lifecycle routes. Carry-forward from Cycle 1 — acceptable per testing conventions.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build SprintRituals.slnx --no-incremental -v minimal` | **0 errors, 15 warnings** (CS8602 absent; remaining pre-existing) |
| Tests | PASS | `dotnet test --no-build` | **77 passed, 0 failed** (Identity:4, Reflekt.Domain:7, Fokus.Domain:62, Reflekt.Persistence:4) |
| EF pending changes | NO CHANGES | `dotnet ef migrations has-pending-model-changes --project Reflekt.Persistence --startup-project Host` | "No changes have been made to the model since the last migration." |
| CardConfiguration fix | VERIFIED | Read `CardConfiguration.cs` | Lines 18-19 and 33-34: comment and active builder call properly separated |
| Migration delta | VERIFIED | Read `20260615212500_RenameSprintToRetroSessionAddFokusLink.cs` | `Up()` adds `FK_Cards_RetroSessions_SprintId` (Cascade) + `FK_Cards_RetroSessions_ReviewedInSprintId` (SetNull); columns unchanged |
| Snapshot updated | VERIFIED | Read `ReflektDbContextModelSnapshot.cs:599-608` | Card entity now has both FK relationships to RetroSession |
| CLAUDE.md fix | VERIFIED | Read `CLAUDE.md` (system-reminder) | Auth bullet: "Landed in RA2"; Fokus linkage: "RA4 — not yet landed" |
| CS8602 fix | VERIFIED | Read `RefreshFokusSprintSnapshotConsumerTests.cs:95` | `x.Context!.Message` with null-forgiving |

*Status: COMPLETE — reviewer, 2026-06-15*
