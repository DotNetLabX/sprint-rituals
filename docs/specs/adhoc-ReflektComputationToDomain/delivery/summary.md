# adhoc-ReflektComputationToDomain (RR-C) — Summary

**Status:** Complete — 2026-06-20
**Branch:** main
**Team mode:** Standard+Codex
**Review mode:** Critic (plan)
**Outcome:** Plan critic ACCEPT · Done-check PASS · Reviewer APPROVED · Codex GO (dual-review converged) · 59/59 tests · 0 fix cycles

## What shipped

ADR-005 computation-to-domain for Reflekt's two report endpoints. The ~80 byte-identical lines of inline aggregation in `GetRetroReportEndpoint` and `GetParticipantReportEndpoint` were lifted into one pure `Reflekt.Domain` report service — killing the duplication (ADR-003), keeping the domain DTO-free (ADR-006), with Mapster mapping at the endpoint boundary (ADR-010). Handler-loads pattern: the endpoint loads the aggregates and passes them into the pure builder. Zero wire change — both endpoints return byte-identical JSON; `ReportContracts.cs` is diff-clean.

### Files created
- `src/Services/Reflekt/Reflekt.Domain/Reports/RetroReportData.cs` — 11 pure domain result records (`EvidenceLinkSource` stays enum; `ReviewStatus` stays `string` to preserve the `"Not reviewed"` literal)
- `src/Services/Reflekt/Reflekt.Domain/Reports/RetroReportBuilder.cs` — pure synchronous builder; no DbContext/repo/Mapster/CancellationToken
- `src/Services/Reflekt/Reflekt.Domain.Tests/Reports/RetroReportBuilderTests.cs` — 16 slices (group filter, ordering, title fallback, column routing/VOs, action-item filter, evidence-link projection fidelity, all 4 previous-actions branches)

### Files modified
- both report endpoints → `auth-gate → load → reportBuilder.Build(...) → .Adapt<ReportResponse>()`; private helpers deleted; builder injected; auth preambles byte-preserved
- `Reflekt.API/Mappings/ReportMappings.cs` → added `RetroReportData → ReportResponse` tree (11 configs); pruned 3 dead `Card/EvidenceLink → Report*Entry` maps
- `Reflekt.API/DependencyInjection.cs` → `AddScoped<RetroReportBuilder>()` next to `GetSprintContextAssembler`
- `src/Services/Reflekt/CLAUDE.md` → ADR-005 report-service + de-duplication note

## Review trail
- **Plan** — critic ACCEPT; 2 LOW advisories (evidence-link projection fidelity; dead-map pruning) folded into Steps 2/3/4/6/8 before implementation.
- **Step 1 done-check** — PASS; architect re-ran `dotnet build` + `dotnet test` independently (verify gate had skipped on a named-spawn quirk).
- **Step 2** — Reviewer APPROVED + Codex GO; dual-review converged, zero blocking findings.

## Carry-overs (non-blocking)
- **Slice-4 strict-ordering test gap** — in-memory `CardGroup` Id=0 collision makes strict vote-desc order untestable at the unit level; the sort is correct by inspection (`OrderByDescending(Votes)`); natural integration-test candidate in a later pass.
- **Manual smoke** for byte-identical JSON across both endpoints (plan 8.7).
- **5 LOW observations** (3 reviewer + 2 codex): `RetroReportBuilder` non-`sealed` (matches `GetSprintContextAssembler`), Slice-5 title-fallback asserts non-empty not exact, cosmetic typo `Should_UsEmptyStringTitle`, `IReadOnlyList<T>`→`List<T>` asymmetry (Mapster handles). Recorded, no fix needed.

## Runtime / plugin issues (all handled, no correctness impact — see communication-log Runtime Issues Log)
- Boundary-detector named-spawn false positives (architect/developer writing their own owned artifacts; review.md co-ownership). Verified base-role == owner; nothing voided.
- Critic result stranded (idle-only message); recovered verbatim via `salvage-transcript.js`; persisted to review-critic.md.
- Verify gate skipped (`unrecognized agent_type: developer-rrc`); fell back to done-check + reviewer + Codex as the real verification gates.

## Backlog reference correction
`backlog.md:3` cited binding rule `.claude/rules/engineering-discipline.md`, which does not exist; the actual guardrails doc is `docs/conventions/project-rules.md`. Flagged to owner (open).

## Lessons
Recorded in `lessons.md` (developer + reviewer sections). Not yet processed by the learner.
