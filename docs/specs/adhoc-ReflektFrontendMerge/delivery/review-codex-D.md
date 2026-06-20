# RA5-D Review (Codex)

> Recovered by the team lead from the Codex runtime job log (`task-mqmc1bkn-4buxx0.log`) —
> Codex computed the verdict but its file write was blocked by the read-only session sandbox
> (known strand). Content below is verbatim from the run.

**Verdict: GO** — all six requested checks CONFIRMED PASS. No findings, no blockers.

## Summary
`BuildTopScopeAdditions` orders by `AddedAt` descending and caps at 5; that order passes through with no downstream re-sort; `ScopeAdditionTotal` is counted from the same factored population before `Take(5)`; `MidSprintAdditionCount` uses case-insensitive `"added"` with `!IsExcluded`; the assembler computes `max(0, total − shown)` only outside the green band; `AddedAt` stays off the wire; the scope-addition list population is intentionally separate from the disruption-metric population.

## Findings (all PASS)

| # | Check | Result |
|---|-------|--------|
| 1 | Rule 7 ordering | PASS — `OrderByDescending(m => m.AddedAt).Take(5)` (SprintAnalyticsGrpcService.cs:260-273); assembler `Take(3)` with no re-sort. |
| 2 | True total, same predicate, no drift | PASS — `GetScopeAdditions(...).ToList()` single population; `ScopeAdditionTotal = scopeAdditions.Count` before the cap; bugs/removed excluded identically. |
| 3 | Case-bug fix | PASS — `string.Equals(Action,"added",OrdinalIgnoreCase) && !IsExcluded`; analyzer emits lowercase; removed not counted. |
| 4 | Assembler overflow | PASS — `Math.Max(0, ScopeAdditionTotal − displayed.Count)`, non-green branch only, no off-by-one (tests pin 9−3=6 and 3−3=0). |
| 5 | Privacy / additive contract | PASS — `AddedAt` ordering-key only, never projected; `ScopeAdditionEntry`/`ScopeAdditionDto` exactly {TicketKey, Summary, StoryPoints}; `ScopeAdditionTotal` additive at Order=16, Orders 1-15 untouched. |
| 6 | Population separation | PASS — list/total = post-planning+not-removed+non-bug; `MidSprintAdditionCount` = event-table `"added"` rows with `!IsExcluded` over all `!WasCommitted`. Not conflated. |

## Evidence gaps
- `[NOT FOUND]` No standalone `*.proto` for `AnonymizedSprintSummary` — expected: this repo uses code-first protobuf-net.Grpc (`Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`). Non-issue.

**TL note:** Codex GO agrees with the reviewer's APPROVE. The reviewer's lone LOW (Fokus caps list at 5, assembler re-trims to 3 → 2 unused entries on the wire) is a harmless efficiency nit, recorded as a follow-up.
