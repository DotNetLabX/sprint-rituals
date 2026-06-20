# RA5-D — Rule 7 Ordering + True Total (PC3 closed: thin)

**Parent:** `plan.md` · **Depends on:** RA5-B (the panel renders what this changes) — but the backend change is independent and can land before/after B; **soft** on RA5-C (touches `AnonymizedSprintSummary`/assembler, not the renamed routes). · **Type:** Scoped cross-module (Fokus gRPC + contract + Reflekt assembler)

Finish the RA4 carry-overs:
- **Rule 7 — most-recently-added ordering + true total.** RA4 shipped scope-additions ordered by **story points** with overflow "+N among top-5-by-SP". Rule 7 wants **most-recently-added first** and a **true** "+N more" (total additions − shown). Now feasible: `SprintMembership.AddedAt` (`Fokus.Domain/Sprint/SprintMembership.cs:8`) exists and is already used by `ScopeChangeAnalyzer`.
- **PC3 — RESOLVED, no code.** Owner decision (2026-06-20): **keep `cached` sections thin** (name/dates + last-known badge, no metric numbers). This closes the RA4 carry-over and ratifies the thin-snapshot design (ADR-019); `FokusSprintSnapshot` stays identity+state only. Recorded here so it isn't re-opened — **not** a deferral.

**Found defect (fold the fix in — we're in the code):** `MidSprintAdditionCount` always returns **0** — `MapToDisruptionMetrics` (`SprintAnalyticsGrpcService.cs:265`) matches `"Added"` but the event table writes lowercase `"added"` (`ScopeChangeAnalyzer.cs:404`). Independent of Rule 7's path (we compute the total inline), but a real correctness bug in the same area.

**What must NOT change:** the privacy boundary (anonymized aggregates only — no AddedAt timestamp or person crosses the wire; only the *ordering* and a *count* change); existing `AnonymizedSprintSummary` field order 1–15 (the new field is additive at the end); the panel UI (RA5-B renders the list/overflow as the contract returns them — this just changes their content).

---

## Step 1 — Fokus: most-recently-added ordering + true total + case-bug fix

In `Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`:
- `BuildTopScopeAdditions` (`:240-252`): change `OrderByDescending(m => m.StoryPoints ?? 0m)` → `OrderByDescending(m => m.AddedAt)` (most-recently-added first), keep `Take(5)`. The wire still carries only `{TicketKey, Summary, StoryPoints?}` — **no `AddedAt` crosses the boundary** (it's an ordering key only, privacy preserved).
- Compute the **true total** over the **identical `.Where(...)` predicate** `BuildTopScopeAdditions` uses for the displayed list — `m.AddedAt > planningCutoff && m.RemovedAt == null && m.Ticket?.IssueType != "Bug"` (`:242-245`) — counted **before** `Take(5)` (critic H1). The total population MUST match the list population, else "+N more" promises tickets the list can never show (bugs / removed). Do **not** route through `MidSprintAdditionCount` (different, broader population + the bug below).
- **Fix the case bug** at `:265` **case-insensitively** to close the whole class, not just one literal: `string.Equals(e.Action, "added", OrdinalIgnoreCase)` — the event table writes lowercase `"added"` (`ScopeChangeAnalyzer.cs:404`). **Sibling (critic H2):** `"removed"` is written lowercase too (`:413`); `MapToDisruptionMetrics` has no `"Removed"` comparison today (`BugAddedSp`/`BugDisruptionRate` are hard-`0m`), so none is *currently* broken — apply the same `OrdinalIgnoreCase` guard to any action compare in this map so a future removed-count can't silently ship the same bug. Record that no `"Removed"` consumer exists today.

**Accept:** a unit test pins ordering = `AddedAt` desc; a test asserts the true total counts the **same predicate** as the list — specifically that a sprint with mid-sprint **bug** additions has those bugs **excluded** from the total (total == non-bug additions, not all additions), so `total − shown` never promises an unshowable ticket; the case-bug-fix test asserts `MidSprintAdditionCount > 0` for a sprint with additions. `Satisfies:` Rule 7 + AC4 (RA4 spec).
**Skill:** None (gRPC-service adaptation of the existing `ScopeChangeAnalyzer` output; reference the current `BuildTopScopeAdditions`) · **TDD:** yes · **Confidence:** high (the field exists; one ordering swap + a count)

## Step 2 — Contract: additive true-total field on `AnonymizedSprintSummary`

In `Blocks.Contracts` `AnonymizedSprintSummary` (fields 1–15 today): add `ScopeAdditionTotal` (`int`) as `[DataMember(Order = 16)]` — additive, backward-compatible, mirroring RA4's +7-field enrichment. Populate it from Step 1's true count in the Fokus server mapping.

**Accept:** the field serializes at Order 16; existing 15 fields/orders unchanged; `dotnet build` green; an existing gRPC round-trip test (or a new one) sees the populated total.
**Skill:** None (additive `[DataMember]` to an existing contract; reference RA4's `AnonymizedSprintSummary` enrichment — not a new contract, so `create-grpc-contract` is heavier than needed) · **TDD:** no (DTO field) · **Confidence:** high

## Step 3 — Reflekt assembler: true "+N more" overflow

In `Reflekt.API/.../GetSprintContext/GetSprintContextAssembler.cs`: set `SprintContextSection.AddedMidSprintOverflow = max(0, ScopeAdditionTotal − AddedMidSprint.Count)` (true total − shown), replacing the current "+N among top-5-by-SP" computation. The `AddedMidSprint` list now arrives most-recently-added-first from Fokus (Step 1) — pass it through in order. **Keep the overflow inside the existing non-green branch** (`:95-96` returns `overflow = null` for green-band sprints — don't compute it above the disruption gate; no block, no overflow). **Update the now-stale doc-comments** at `:88, 103-104` that assert "+N among the top-5-by-SP … not the true total" — they invert the new behavior (critic M).

**Accept:** a `Reflekt.Persistence.Tests`/assembler test asserts overflow = total − shown (e.g. 9 additions → list of 5 + overflow 4) and that list order is preserved from the contract. `Satisfies:` Rule 7 + RA4 AC4 ("+N more" true count).
**Skill:** None (assembler mapping; reference the existing `GetSprintContextAssembler` overflow logic) · **TDD:** yes · **Confidence:** high

## Step 4 — Verification

- `dotnet build SprintRituals.slnx` green; Fokus + Reflekt test projects green.
- The sprint-context endpoint now returns scope additions most-recently-added-first with a true `addedMidSprintOverflow`; the RA5-B panel's "+N more" reflects the real remaining count.
- PC3 close-out recorded (thin cached — no change); RA4 carry-over list updated to Done.

**Accept:** all bullets pass; documented in implementation.md (ordering + true-total + case-bug fix evidence; PC3 closed).
**Skill:** None · **TDD:** N/A (verification) · **Confidence:** high

---

## Skill Mapping

| Step | Disposition | TDD | Notes |
|------|-------------|-----|-------|
| 1 | None | yes | Fokus gRPC ordering→`AddedAt` + true count + `"added"` case-bug fix |
| 2 | None | no | Additive `[DataMember(Order=16)]` on `AnonymizedSprintSummary` (RA4 enrichment pattern) |
| 3 | None | yes | Assembler overflow = total − shown; preserve contract order |
| 4 | None | N/A | Build + test verification; PC3 close-out |

Steps 1 & 3 are `TDD: yes` (ordering / count / overflow are testable) — the developer invokes `tdd` on both; the done-check expects `tdd` in the skill log for this run. No pattern skill matches (the analyzer + contract + assembler all exist — this adapts them); `Satisfies:` cites Rule 7 / AC4 of the RA4 spec.

## Review mode

**Critic — code-grounded.** Verify against source: `AddedAt` exists and the ordering swap is correct; the true-total is computed over the same predicate as the displayed list; the contract field is additive (orders 1–15 untouched); **no `AddedAt`/timestamp crosses the privacy boundary** (only ordering + a count). Confirm the case-bug fix matches the analyzer's actual literal.

---

## Plan Review (critic, Mode 2 — code-grounded)

**Verdict: REVISE → both HIGH findings folded above.** Critic agent `ae12595c6d0fc870c`. Coverage: Rule 7 ordering is correct and ADR-019 privacy holds (no `AddedAt`/timestamp on the wire — confirmed), contract additivity confirmed (Order 16 free). The two HIGH findings were correctness gaps in *how* the true total is computed — both now pinned.

| # | Severity | Finding | Disposition |
|---|----------|---------|-------------|
| H1 | HIGH | "True total" population unpinned — three disagreeing addition sets in the code; counting a broader set than the list makes "+N more" promise unshowable (bug/removed) tickets, reintroducing the AC4 bug | Step 1 pinned to the **identical `.Where` predicate** as `BuildTopScopeAdditions` (post-planning, not-removed, not-Bug), count before `Take(5)`; accept test asserts bugs excluded from the total |
| H2 | HIGH | Sibling `"removed"` case mismatch left unfixed (plan said "check Removed too" but body fixed only `"Added"`) | Step 1 now closes the class with `OrdinalIgnoreCase`; records that no `"Removed"` consumer exists today |
| M | MEDIUM | Stale assembler doc-comments (`:88,103-104`) assert "+N among top-5-by-SP" — invert the new behavior | Step 3 updates the comments |
| L | LOW | `overflow=null` on green band (`:95-96`) — don't compute above the disruption gate | Step 3 keeps overflow inside the non-green branch |

**Confirmed clean (no change):** privacy boundary holds (`ScopeAdditionEntry` = `{TicketKey, Summary, StoryPoints?}` only, `AddedAt` never serialized); `[DataMember(Order=16)]` at end-of-type is backward-compatible; the assembler receives the new field via `summary`; PC3 correctly a no-op.

**Status: Approved — ready for developer** (both HIGH folded; Rule 7 + AC4 now correctly delivered within ADR-019; no open CRITICAL).
