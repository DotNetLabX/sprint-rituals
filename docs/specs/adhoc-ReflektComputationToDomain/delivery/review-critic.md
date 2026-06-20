# Critic — Plan Review: RR-C (adhoc-ReflektComputationToDomain)

> Recovered by team-lead via transcript salvage (critic returned only an idle notification; `TaskOutput` "no task found" on both id forms). Verbatim record of the critic's findings — the critic writes no file by design (Relay Contract).

**VERDICT: ACCEPT** (with 2 LOW advisory notes for the developer — neither blocks)

The plan is unusually well-grounded. Every scope requirement from backlog RR-C and the triage maps to a numbered step, every binding ADR (005/010/006/003) is satisfied with the correct in-repo precedent, and the behavior-preservation hazards are not just acknowledged but pinned with binding notes. Verified the plan's source-code claims against the actual files — they hold.

### Scope → step coverage (complete)

| RR-C requirement | Plan step | Verified |
|---|---|---|
| Lift `Reports/*` aggregation into a `Reflekt.Domain` report service | Step 2 (`RetroReportBuilder`) | ✓ |
| Return domain types (ADR-005/006) | Step 1 (`RetroReportData` tree) | ✓ no API/Contracts type |
| Mapster-map at endpoint boundary (ADR-010) | Step 3 (`ReportMappings`) | ✓ |
| Thin the 132/131-line handlers | Step 4 (auth→load→build→adapt) | ✓ |
| Problem-class root fix — kill the ~80-line duplication (ADR-003) | Step 4 deletes both private helpers | ✓ |

### Behavior-preservation verification (the core risk)

Read both endpoints. **Lines 49–130 are byte-identical** between `GetRetroReportEndpoint` and `GetParticipantReportEndpoint` — the plan's central claim is true, so the verbatim lift is a single source, not a reconciliation of two drifting copies. The auth preambles genuinely differ only in the `session.TeamId != req.TeamId` guard and the `req.TeamId` vs `session.TeamId` keying, exactly as the plan states. The `ReviewStatus` enum is `{None, Done, CarriedOver, Dropped}` — confirming `"Not reviewed"` is a literal, not a member, so the plan's binding note on keeping `ReviewStatus` a `string` in the domain result is correct and necessary.

### Two LOW advisories (developer-time, not blockers)

1. **Evidence-link projection fidelity (the one real hazard).** The current `.Adapt<ReportCardEntry>()` / `.Adapt<ReportActionItemEntry>()` populate `.EvidenceLinks` via Mapster's *nested-collection* mapping, driven by the registered `EvidenceLink → ReportEvidenceLinkEntry` map (`Source.ToString()`, plus `Url/Title/ExternalKey`). When Step 2 replaces these with explicit `ReportCardEntryData(...)` construction, the builder must project **all** of `Card.EvidenceLinks` in list order, carrying `Url, Source (enum), Title, ExternalKey` — and **nothing else** (Mapster maps by name; `Description/IconUrl/ImageUrl/FetchStatus/FetchedAt` are silently dropped today because the wire record has no such members). The plan implies this but never states the field set or that order = `_evidenceLinks` insertion order. Recommend the developer add one builder test asserting evidence-link field set + order, and the plan's Step 6 evidence-link test be strengthened beyond just "Source carries the enum." Risk if missed: silent JSON drift in the `evidenceLinks` array. Not a plan defect — an under-specified detail worth pinning before the cut.

2. **Pruning the old `Card → Report*Entry` maps.** Step 3 / Open-Q correctly gate removal of the now-orphaned `Card→ReportCardEntry`, `Card→ReportActionItemEntry`, and `EvidenceLink→ReportEvidenceLinkEntry` maps on a grep showing zero callers. Confirmed the **only** four `Adapt<Report*Entry>` call-sites are the four lines inside these two endpoints — so after Step 4 all three maps become dead. The plan's "remove only if grep confirms" default is right; just noting the grep *will* confirm, so removal is the expected outcome, not a maybe.

### Non-blocking flags (already raised by the plan, critic concurs)

- The `.claude/rules/engineering-discipline.md` reference gap is real (no such file; `docs/conventions/project-rules.md` is the actual doc). The plan correctly notes the binding ADRs are intact regardless. Reconciliation is a team-lead docs item, not a plan blocker.
- The de-duplication being credited as intended ADR-003 work (not accidental) — concur; the reviewer should score it as such.

**Nothing in the plan would produce a wire-contract or behavior change if followed as written.** `ReportContracts.cs` is correctly fenced as out-of-scope, the pre-load short-circuit equivalence (Step 4 binding note) correctly preserves the "no previous-actions section" path, and the previous-session id argument (`previousSession.Id` for unreviewed, current `retroSessionId` for reviewed) matches the original.
