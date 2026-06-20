# RR-C — Reflekt Computation→Domain (ADR-005)

**Feature Spec:** None (adhoc refactoring pass — scope in `docs/backlog.md:119` row RR-C + `docs/proposals/reflekt-refactor-triage.md` RR-C row; the gate is the **user-point register ADR-005/010** via the triage's problem-class framing **ADR-003**. No open questions — triage O1 Resolved 2026-06-20.)
**Slug:** adhoc-ReflektComputationToDomain
**Skill:** `domain-service` (primary) + `analytics-computation-service` (naming reference)
**Intent:** Refactoring (restructure; behavior must not change)

> **Binding-doc note (not blocking):** the task brief and `docs/backlog.md:3` cite `.claude/rules/engineering-discipline.md` as a binding constraint. **That file does not exist in this repo** — there is no `.claude/rules/` directory. The actual checked-in guardrails doc is `docs/conventions/project-rules.md`. The binding ADRs (005/010 + 003 problem-class in `docs/architecture/decisions.md`) are intact and are what this plan is gated on. Flagged to the team-lead for reference reconciliation.

## Context

ADR-005 (computation lives in the Domain layer) + ADR-010 (endpoint owns the response DTO; Mapster-map domain result → DTO at the boundary), applied as a **problem class** per ADR-003. The Phase-1 scan (architect) confirmed the problem class has exactly **two** instances, and they are the **same** instance twice over: the two `Reports/*` endpoint handlers compute report aggregation inline and are **near-byte-identical** from the data-load point onward.

- `GetRetroReportEndpoint.cs` (132 lines) — `GET /api/teams/{teamId}/retro-sessions/{retroSessionId}/report`
- `GetParticipantReportEndpoint.cs` (131 lines) — `GET /api/retro-sessions/{retroSessionId}/report`

Lines **~49–130 are identical** between the two files: the `cardsByGroup` grouping, the `groupEntries` filter (`IsDiscussed || Votes > 1`, order by votes desc), the per-column `BuildColumnSection` assembly, the action-item projection, and the `BuildPreviousActionsSectionAsync` (previous-retro action review). Only the **auth/ownership preamble** (the first ~24 lines: the `participantId`-claim vs team-owner gate, and `GetRetroReport`'s extra `session.TeamId != req.TeamId` guard) differs between them. That whole shared computation block is **report aggregation = domain logic** (ADR-005), currently in the API layer, and **duplicated** (a DRY defect on top of the ADR violation).

The fix lifts the shared aggregation into **one pure `Reflekt.Domain` report service** that returns domain result records, and reduces both endpoints to: **auth gate → load aggregates → call the service → `.Adapt<ReportResponse>()`**. The two endpoints keep their distinct auth preambles (those are HTTP/authorization concerns, not domain logic — they stay in the endpoints). Extracting the shared block also kills the duplication at the root, satisfying ADR-003 (fix the class, not the symptom).

This mirrors the in-repo Fokus precedent exactly: `adhoc-Pass3c-ComputationToDomain` / ADR-005 — `Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs` (pure calculator, no `DbContext`/repo/HTTP) + `SprintSummaryResult.cs` (domain result records, `*Data` suffix) with the endpoint Mapster-mapping the domain result to the wire DTO (`SprintSummaryResponse`). RR-C is the smaller, simpler analogue — one service, one result-records file, two thin endpoints.

**`domain-service` shape (the binding rule for the calculator):** pure by default — **no `DbContext`, no repository, no HTTP dependency**. The **endpoint handler loads** the aggregates and **passes loaded aggregates in** (per ADR-005 and the `domain-service` skill: the handler does the loading, the domain service computes). The service is named by what it does and returns **domain types** (the new `RetroReportData` record tree), never the API response DTO (ADR-006 — domain stays DTO-free, which Reflekt already honors; this pass must not regress it).

## Scope

**In scope:**
1. Create `RetroReportData` + its sub-records (column section, group entry, card entry, evidence-link entry, action-item entry, previous-actions section, previous-action entry, plus the retro-session/team/participant info records) in `Reflekt.Domain/Reports/` — domain result types holding domain values (enums stay enums, e.g. `EvidenceLinkSource`, `ReviewStatus`).
2. Create `RetroReportBuilder` in `Reflekt.Domain/Reports/` — a pure domain service that takes the **loaded aggregates** (session, cards, card-groups, vote counts, participants, team, and the previous-session review inputs) and returns `RetroReportData`. It contains the lifted lines ~49–130 logic verbatim (grouping, the `IsDiscussed || Votes > 1` filter + vote-desc order, `BuildColumnSection`, action-item projection, `BuildPreviousActionsSection`).
3. Extend `ReportMappings.cs` (`IRegister`) with the domain-result → `ReportResponse` maps (the `EvidenceLinkSource → string` stringify moves here; it currently maps `EvidenceLink.Source.ToString()`).
4. Rewrite both endpoints to: keep their (distinct) auth preamble → load the aggregates the builder needs → call `RetroReportBuilder` → `.Adapt<ReportResponse>()` → `Send.OkAsync`. Remove the per-endpoint `BuildColumnSection` / `BuildPreviousActionsSectionAsync` private methods (now in the domain service).
5. Register `RetroReportBuilder` in `Reflekt.API/DependencyInjection.cs` (`AddScoped`), mirroring the existing `GetSprintContextAssembler` registration.
6. New domain unit tests for `RetroReportBuilder` in `Reflekt.Domain.Tests/Reports/` (the builder is pure → directly testable, no DB).
7. Update `src/Services/Reflekt/CLAUDE.md` to record the ADR-005 report-service pattern.
8. Verification sweep + build + tests.

**Explicitly out of scope (do NOT touch):**
- **The wire contract.** `ReportResponse` and every sub-record in `Reports/ReportContracts.cs` (`ReportRetroSessionInfo`, `ReportTeamInfo`, `ReportParticipantInfo`, `ReportColumnSection`, `ReportGroupEntry`, `ReportCardEntry`, `ReportActionItemEntry`, `ReportEvidenceLinkEntry`, `ReportPreviousActionsSection`, `ReportPreviousActionEntry`) keep their **exact current shape and property names**. Both report endpoints return byte-identical JSON. No client/Vue change. The Reflekt SPA is not in scope.
- **The auth/ownership preambles.** Each endpoint's `participantId`-claim check, the team-owner fallback, the `Send.NotFoundAsync`/`Unauthorized`/`Forbidden` branches, and `GetRetroReport`'s `session.TeamId != req.TeamId` guard are **authorization** (HTTP concern), not domain logic — they stay in the endpoints, unchanged. Do NOT move them into the domain service.
- **`TimerService`** (`Reflekt.API/Features/RetroSessions/Timer/`) — triage-confirmed by-design: a real-time concern, not computation-in-API. Do NOT touch.
- **Other `*Service`/assembler/cache classes** — `PhaseActionCache`, `GetSprintContextAssembler`, `BroadcastPhaseChangedHandler`. Phase 1 scanned them; none is inline report/analytics aggregation. Out of scope.
- **The repositories.** The handlers already load via the existing `Card/CardGroup/Vote/Participant/Team/RetroSessionRepository` methods. The builder takes the **loaded results**, not the repos — no repository method changes, no new query methods, no repo dependency in the domain.
- **Any behavior, filter threshold, ordering, null-handling, or default change.** Pure structural lift: same `IsDiscussed || Votes > 1` filter, same vote-desc ordering, same `g.Title ?? firstCard.Text ?? ""` title fallback, same "section omitted when no eligible previous actions" rule, same `"Not reviewed"` literal. No observable JSON difference for either endpoint.
- **RR-D** (event-correctness) — not pre-empted.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | `domain-service` (result-type shape) + `analytics-computation-service` (naming) | Follow | yes | The result-record tree names/members under `Reflekt.Domain/Reports/`; domain values stay domain (`EvidenceLinkSource`/`ReviewStatus` enums, not strings); parallels `ReportResponse` shape | — |
| 2 | `domain-service` (pure cross-aggregate service) | Follow | yes | `RetroReportBuilder` takes loaded aggregates (param list in Domain Model Changes); lifts lines ~49–130 verbatim; returns `RetroReportData`; no `DbContext`/repo/HTTP dep | — |
| 3 | `domain-patterns` (Mapping convention / Mapster `IRegister`) | Follow | no | Add `RetroReportData → ReportResponse` (+ nested) maps to `ReportMappings.cs`; move the `EvidenceLinkSource → .ToString()` map here | — |
| 4 | `extract-feature-service` (endpoint → service call shape) | Follow | yes | Both endpoints: keep auth preamble, load, call builder, `.Adapt<ReportResponse>()`; delete the now-duplicated private helpers | — |
| 5 | `service-registration` | Follow | no | `AddScoped<RetroReportBuilder>()` in `AddReflektModule` (mirror `GetSprintContextAssembler`) | — |
| 6 | `tdd` | Follow | yes | `RetroReportBuilder` unit tests in `Reflekt.Domain.Tests/Reports/` — pure inputs, no DB | — |
| 7 | (none) | — | no | `Reflekt/CLAUDE.md` ADR-005 report-service note | — |
| 8 | (none) | — | no | Verification sweep + build + test run | — |

**Disposition notes:**
- **Step 1 Follow `domain-service` + `analytics-computation-service`** — `domain-service` gives the "returns domain types, no DTO" rule; `analytics-computation-service` gives the **naming** guidance (a calculator/builder named by what it computes, `*Data`/`*Result` result records). The plan supplies the record tree; the developer invokes the skills for the shape. The Fokus `SprintSummaryResult.cs` is the in-repo template for the `*Data` record family.
- **Step 2 Follow `domain-service`** — the binding shape: pure, handler-loads, returns domain types. `Fokus.Domain/Analytics/SprintSummaryAnalyzer.cs` is the in-repo precedent (takes loaded `Sprint`/`Developer`/`Ticket` lists, returns `SprintSummaryData`). `RetroReportBuilder` is the smaller analogue.
- **Step 3 Follow `domain-patterns`** (Mapping convention, ADR-010) — `ReportMappings.cs` is already a live `IRegister`; this extends it with the result→DTO maps. The `EvidenceLink → ReportEvidenceLinkEntry` map already stringifies `Source`; the new map stringifies the same enum from the domain result.
- **Step 4 Follow `extract-feature-service`** — the skill covers reducing a fat `HandleAsync` to a service call with the result-record pattern. Here the "feature service" is a **domain** service (stronger than the skill's API-layer default, per ADR-005), but the endpoint-shrink shape (inject, load, call, map) is exactly the skill's pattern. The plan binds the domain-layer placement.
- **Step 5 Follow `service-registration`** — one `AddScoped` in `AddReflektModule`; the live `GetSprintContextAssembler` line is the precedent.
- **Step 6 Follow `tdd`** — the builder is pure → unit-testable with hand-built aggregate inputs (no SQLite needed). Red-green per the skill.
- **Steps 7, 8 None** — a doc edit and the verification battery.
- **No "Build."** All infrastructure exists: `Reflekt.Domain` project (refs only `Blocks.Domain` — keep it that way), Mapster (`ReportMappings` is a live `IRegister`; `.Adapt<>` already used in both endpoints), `Reflekt.Domain.Tests`, the DI module. No new package, no new project.

## Domain Model Changes

New report result records + one pure domain service, all under `src/Services/Reflekt/Reflekt.Domain/Reports/`. **`Reflekt.Domain` references only `Blocks.Domain`** — these records use only primitives + Reflekt domain enums; no API/Contracts type may enter (ADR-006, currently clean — do not regress).

### Result record tree (`RetroReportData.cs`)

Parallels `ReportResponse` but holds **domain values** (enums stay enums; the string conversion happens in the Mapster map at the boundary). Suggested shape (developer may merge into one file):

| Domain result record | Members | Parallels wire record | Note |
|----------------------|---------|----------------------|------|
| `RetroReportData` | `RetroSessionInfo`, `TeamInfo`, `IReadOnlyList<ParticipantInfo> Participants`, `IReadOnlyList<ReportColumnSectionData> Columns`, `IReadOnlyList<ReportActionItemData> ActionItems`, `ReportPreviousActionsData? PreviousActionsReview` | `ReportResponse` | root |
| `ReportRetroSessionInfoData` | `int Id, string Name, DateTime CreatedAt, DateTime? ClosedAt` | `ReportRetroSessionInfo` | |
| `ReportTeamInfoData` | `int Id, string Name` | `ReportTeamInfo` | |
| `ReportParticipantInfoData` | `string DisplayName` | `ReportParticipantInfo` | |
| `ReportColumnSectionData` | `string ColumnName, string ColumnColor, IReadOnlyList<ReportGroupEntryData> Groups` | `ReportColumnSection` | `ColumnColor` is already `string` (the `.Color.Value`) — keep as string here |
| `ReportGroupEntryData` | `string GroupTitle, int VoteCount, bool IsDiscussed, IReadOnlyList<ReportCardEntryData> Cards` | `ReportGroupEntry` | |
| `ReportCardEntryData` | `string Text, IReadOnlyList<ReportEvidenceLinkData> EvidenceLinks` | `ReportCardEntry` | |
| `ReportActionItemData` | `string Text, string? AssigneeName, IReadOnlyList<ReportEvidenceLinkData> EvidenceLinks` | `ReportActionItemEntry` | |
| `ReportEvidenceLinkData` | `string Url, EvidenceLinkSource Source, string? Title, string? ExternalKey` | `ReportEvidenceLinkEntry` | **`Source` is the `EvidenceLinkSource` enum** — stringified in the Mapster map, not here |
| `ReportPreviousActionsData` | `string PreviousRetroSessionName, IReadOnlyList<ReportPreviousActionData> Items` | `ReportPreviousActionsSection` | |
| `ReportPreviousActionData` | `string Text, string? AssigneeName, string ReviewStatus` | `ReportPreviousActionEntry` | `ReviewStatus` is already projected to a `string` today (`card.ReviewStatus.ToString()` / `"Not reviewed"`) — keep as string to preserve the exact literal `"Not reviewed"` for unreviewed items |

> **`ReviewStatus` projection (binding — preserves the `"Not reviewed"` literal):** the current code emits `card.ReviewStatus.ToString()` for reviewed items and the literal string `"Not reviewed"` for unreviewed-eligible ones — these are **not** the same enum (`"Not reviewed"` is not a `ReviewStatus` member). So `ReportPreviousActionData.ReviewStatus` stays a **`string`** computed inside the builder (preserving both the `.ToString()` and the `"Not reviewed"` literal), NOT an enum stringified at the boundary. This is the one field where the string projection stays in the domain service — because it's a domain decision (reviewed vs not-reviewed), not a wire-format choice.

### `RetroReportBuilder.cs` — pure domain service (follow `domain-service`)

A class in `Reflekt.Domain.Reports` with one public method, e.g.:

```
public RetroReportData Build(
    RetroSession session,
    IReadOnlyList<Card> cards,
    IReadOnlyList<CardGroup> groups,
    IReadOnlyDictionary<int, int> voteCountsByGroupId,
    IReadOnlyList<Participant> participants,
    Team team,
    IReadOnlyList<Card> reviewedPreviousActions,      // GetActionItemsReviewedInRetroSession(currentRetroSessionId)
    IReadOnlyList<Card> unreviewedEligiblePreviousActions, // GetUnreviewedActionItemsByRetroSessionId(previousSessionId)
    RetroSession? previousSession)                    // GetMostRecentClosedByTeamId(team) — null ⇒ no previous-actions section
```

- Body = the lifted lines ~49–130 from either endpoint (they are identical): `cardsByGroup` grouping (Note cards with a `GroupId`, ordered by `CreatedAt`), `groupEntries` (title fallback `g.Title ?? firstCard.Text ?? ""`, votes from the dict, `IsDiscussed = g.DiscussedAt.HasValue`, filter `IsDiscussed || Votes > 1`, order votes desc), the 3 `BuildColumnSection` calls (`session.Column1.Name`/`session.Column1.Color.Value` etc. — the VOs from RR-B), the action-item projection (`CardType.ActionItem`), and the previous-actions assembly (the `BuildPreviousActionsSectionAsync` logic, but **synchronous** — the data is now passed in pre-loaded, so the `async`/`await` repo calls dissolve into the param list above).
- `BuildColumnSection` and the previous-actions assembly become **private static** helpers on the builder (moved verbatim from the endpoints, with `ReportColumnSection`/`ReportGroupEntry`/… → the `*Data` records, and `c.Adapt<ReportCardEntry>()` → a plain `ReportCardEntryData(...)` construction since Mapster lives at the API boundary now, not in the domain).
- **No `DbContext`, no repository, no `CancellationToken`** (pure CPU over in-memory inputs). Returns `RetroReportData`.

> **`Adapt` in the domain (binding — do NOT leak Mapster into the domain):** the current endpoints call `c.Adapt<ReportCardEntry>()` / `c.Adapt<ReportActionItemEntry>()` **inside** the aggregation block. In the domain service these become **explicit constructions** of `ReportCardEntryData`/`ReportActionItemData` (Text + the evidence-link list projected to `ReportEvidenceLinkData`). Mapster (`Adapt`) stays at the API boundary (Step 3) — the domain has no Mapster reference and must not gain one.

### What stays in the endpoints (auth — not domain)

The per-endpoint preamble is unchanged and remains in `HandleAsync`:
- `GetRetroReportEndpoint`: load session; `session is null || session.TeamId != req.TeamId` → `NotFound`; the `participantId`-claim → `retroSessionId`-claim match (`Unauthorized` on mismatch) vs the team-owner fallback (`Forbidden`).
- `GetParticipantReportEndpoint`: load session; `session is null` → `NotFound`; the same `participantId`-claim vs team-owner gate (keyed on `session.TeamId`, no `req.TeamId`).

After the gate passes, each endpoint loads the same aggregate set and calls the builder.

## Data Model Changes

**None.** No EF schema change, no migration, no DB change. Pure code relocation (computation API→Domain) + a Mapster map. `reflekt.db` is untouched.

## Cross-Service Changes

**None.** No gRPC, integration-event, or wire-contract change. The two report endpoints' routes, auth policies, request types, and `ReportResponse` JSON are all preserved. SignalR untouched. No client/Vue change.

## Implementation Steps

**Ordering (binding):** Steps 1→2→3→4 are one logical change but can land build-green incrementally: Step 1 (records) + Step 2 (builder) are pure additions — the solution stays green (old endpoints still compile, still self-contained). Step 3 (Mapster map) is an additive `IRegister` change — still green. Step 4 (rewrite both endpoints + delete their private helpers) is the cut-over; build-green must hold after it (the old inline logic is replaced, not left dangling). Step 5 (DI) pairs with Step 4 (the endpoints inject the builder). Step 6 (tests) follows green. Steps 7 (docs) any time. Step 8 verifies last.

### Step 1 — Create the `RetroReportData` result-record tree

**Disposition: Follow `domain-service` + `analytics-computation-service`** (result-type shape + naming). **Confidence: high** — `Fokus.Domain/Analytics/SprintSummaryResult.cs` is the in-repo template.

Create `src/Services/Reflekt/Reflekt.Domain/Reports/RetroReportData.cs` with the record tree in **Domain Model Changes**. Domain values stay domain: `ReportEvidenceLinkData.Source` is the `EvidenceLinkSource` enum; `ReportPreviousActionData.ReviewStatus` is a `string` (per the binding note — it carries the `"Not reviewed"` literal). All other members are primitives matching the parallel wire record.

- The records live in namespace `Reflekt.Domain.Reports`. `EvidenceLinkSource` resolves from `Reflekt.Domain.Cards` (add the `using` or rely on the file-scoped reference — `Reflekt.Domain`'s `GlobalUsings.cs` covers only `Blocks.Domain.*`, so an explicit `using Reflekt.Domain.Cards;` is needed for the enum).

**Accept:**
- `RetroReportData.cs` exists; the record tree parallels `ReportResponse` member-for-member (same nesting, same names sans the wire-only string conversions).
- `ReportEvidenceLinkData.Source` is `EvidenceLinkSource` (enum, not string). `ReportPreviousActionData.ReviewStatus` is `string`.
- No reference to any `Reflekt.API.*` / `*.Contracts` type (ADR-006 — domain stays DTO-free). `dotnet build` green (pure addition).

**Satisfies:** ADR-005 (domain result types) / ADR-006 (no DTO in domain).

### Step 2 — Create the pure `RetroReportBuilder` domain service

**Disposition: Follow `domain-service`** (pure cross-aggregate service; handler loads). **Confidence: high** — the logic is lifted verbatim from two identical sources; `SprintSummaryAnalyzer` is the precedent.

Create `src/Services/Reflekt/Reflekt.Domain/Reports/RetroReportBuilder.cs` with the `Build(...)` method signature in **Domain Model Changes**. Lift lines ~49–130 from `GetRetroReportEndpoint.cs` (identical to `GetParticipantReportEndpoint.cs`) into `Build` + private static helpers (`BuildColumnSection`, previous-actions assembly), substituting:
- the `*Data` result records for the wire records,
- the passed-in loaded collections for the `await repo...` calls (the previous-actions logic becomes synchronous — the three repo reads it did become the three pre-loaded params + the nullable `previousSession`),
- explicit `ReportCardEntryData(...)`/`ReportActionItemData(...)` construction for the former `.Adapt<...>()` calls (no Mapster in the domain — see the binding note).

Add `using Reflekt.Domain.Cards;` / `using Reflekt.Domain.CardGroups;` / `using Reflekt.Domain.RetroSessions;` / `using Reflekt.Domain.Teams;` / `using Reflekt.Domain.Participants;` as needed.

> **Evidence-link projection fidelity (binding — replaces a name-based Mapster map, so the field set must be pinned explicitly):** today the nested evidence links are produced by `c.Adapt<ReportCardEntry>()`/`c.Adapt<ReportActionItemEntry>()`, which Mapster-maps `Card.EvidenceLinks → ReportEvidenceLinkEntry` **by name** — carrying exactly `Url`, `Source` (stringified), `Title`, `ExternalKey`, and **silently dropping** `Description`, `IconUrl`, `ImageUrl`, `FetchStatus`, `FetchedAt` (no matching member on the wire record). The explicit construction in the builder must reproduce this exactly:
> - Project **all** of `card.EvidenceLinks` in **list order** (the `_evidenceLinks` insertion order the aggregate exposes via `EvidenceLinks` — do **not** reorder/sort/filter).
> - Each `ReportEvidenceLinkData` carries **only** `Url`, `Source` (the `EvidenceLinkSource` enum — stringified later at the boundary), `Title`, `ExternalKey` — and **nothing else**. The five dropped fields stay dropped. Adding any of them would be silent JSON drift in the `evidenceLinks` array.

**Accept:**
- `RetroReportBuilder` is a class with one public `Build(...)` returning `RetroReportData`; no `DbContext`, no repository, no `CancellationToken`, no Mapster, no HTTP type in its signature or body.
- The grouping filter (`IsDiscussed || Votes > 1`), vote-desc order, title fallback (`g.Title ?? firstCard.Text ?? ""`), action-item filter, and the previous-actions rule (section null when both reviewed + unreviewed-eligible lists are empty; `"Not reviewed"` literal for unreviewed) are byte-identical to the current endpoint logic.
- Evidence links are projected from **all** of `card.EvidenceLinks` in list order, each carrying exactly `Url`/`Source`(enum)/`Title`/`ExternalKey` and no other field (per the projection-fidelity note above).
- Reads the RR-B VOs correctly (`session.Column1.Name`, `session.Column1.Color.Value`, ×3).
- `dotnet build` green (pure addition; old endpoints still in place).

**Satisfies:** ADR-005 — report aggregation now in the Domain layer, pure, handler-loads.

### Step 3 — Extend `ReportMappings` with domain-result → wire maps

**Disposition: Follow `domain-patterns`** (Mapping convention, ADR-010). **Confidence: high** — `ReportMappings.cs` is a live `IRegister`; the field names align 1:1 so the maps are near-trivial.

Modify `src/Services/Reflekt/Reflekt.API/Mappings/ReportMappings.cs`: add `config.NewConfig<...>()` entries mapping `RetroReportData → ReportResponse` and every nested `*Data → *Entry`/`*Section` pair. The only non-1:1 maps:
- `ReportEvidenceLinkData → ReportEvidenceLinkEntry`: `.Map(d => d.Source, s => s.Source.ToString())` (the enum→string stringify, same shape as the existing `EvidenceLink → ReportEvidenceLinkEntry` map already in this file).
- All other pairs are member-name-identical → Mapster auto-maps (no explicit `.Map`). `ReportPreviousActionData.ReviewStatus` is already a string → direct.

> **The three existing maps go dead after Step 4 (binding — expected outcome, not a maybe):** the only call-sites of `Adapt<ReportCardEntry>`, `Adapt<ReportActionItemEntry>`, and `Adapt<ReportEvidenceLinkEntry>` are the four `.Adapt<...>()` calls **inside the two endpoints** (the per-card action-item projection + the nested evidence-link maps). Step 4 deletes all of them. So after the cut-over the existing `Card → ReportCardEntry`, `Card → ReportActionItemEntry`, and `EvidenceLink → ReportEvidenceLinkEntry` configs have **zero remaining consumers** and are dead. **Remove all three in Step 4's pruning sub-step** — the grep there confirms zero callers (it WILL be zero — there are no other call-sites in the module). Do NOT carry them as "might be used elsewhere": they are not. This step (Step 3) only *adds* the new `RetroReportData → ReportResponse` tree; the dead-map deletion is owned by Step 4 (after the call-sites are gone, so the build never breaks).

**Accept:**
- `ReportMappings.cs` registers `RetroReportData → ReportResponse` + all nested pairs; the `Source` enum→string map is present.
- `ReportResponse` and all wire sub-records in `ReportContracts.cs` are **structurally unchanged** (verify via diff — no field renamed/removed/retyped).
- `dotnet build` green (additive `IRegister` change — the old maps still have their callers until Step 4).

**Satisfies:** ADR-010 — Mapster maps the domain result to the endpoint-owned DTO at the boundary.

### Step 4 — Rewrite both endpoints: auth → load → build → adapt; delete the duplicated helpers

**Disposition: Follow `extract-feature-service`** (endpoint → service-call shape). **Confidence: high** — the auth preamble is preserved verbatim; the body becomes load + one builder call + `.Adapt`.

Rewrite both `HandleAsync` bodies to, after the (unchanged) auth gate:
1. Load the aggregate set the builder needs, via the existing repo methods the handlers already call: `cardRepository.GetByRetroSessionIdAsync`, `cardGroupRepository.GetByRetroSessionIdAsync`, `voteRepository.GetVoteCountsByGroupIdsAsync`, `participantRepository.GetByRetroSessionIdAsync`, `teamRepository.GetByIdAsync`, plus the previous-actions trio: `retroSessionRepository.GetMostRecentClosedByTeamIdAsync(teamId)` → if non-null, `cardRepository.GetActionItemsReviewedInRetroSessionAsync(currentRetroSessionId)` + `cardRepository.GetUnreviewedActionItemsByRetroSessionIdAsync(previousSession.Id)`; if the previous session is null, pass empty lists + null `previousSession` (the builder reproduces the "no section" rule).
2. Call `reportBuilder.Build(session, cards, groups, voteCounts, participants, team!, reviewed, unreviewedEligible, previousSession)`.
3. `var response = result.Adapt<ReportResponse>(); await Send.OkAsync(response, ct);`

Inject `RetroReportBuilder reportBuilder` into both endpoint constructors. Delete the now-duplicated private `BuildColumnSection` and `BuildPreviousActionsSectionAsync` methods from both endpoints (moved into the builder). Remove the `using Reflekt.Domain.Cards;` only if `CardType` is no longer referenced in the endpoint after the cut (the action-item filter moved to the builder — verify).

**Dead-map pruning sub-step (binding — owned here because the call-sites die here):** after both endpoints are rewritten, grep `Adapt<ReportCardEntry>`, `Adapt<ReportActionItemEntry>`, `Adapt<ReportEvidenceLinkEntry>` across `src/Services/Reflekt` (excluding `obj`/`bin`). The four call-sites just deleted were the only consumers, so this returns **zero**. Then delete the now-dead `config.NewConfig<Card, ReportCardEntry>()`, `config.NewConfig<Card, ReportActionItemEntry>()`, and `config.NewConfig<EvidenceLink, ReportEvidenceLinkEntry>()` registrations from `ReportMappings.cs` (the new `RetroReportData → ReportResponse` tree from Step 3 stays). If the grep returns non-zero, a call-site was missed — fix it before pruning.

> **Pre-load equivalence (binding):** today `BuildPreviousActionsSectionAsync` does `GetMostRecentClosedByTeamIdAsync` first, and **only** if non-null does it run the two action-item queries; and it returns null (no section) when both lists are empty. The endpoint must replicate this **load-time short-circuit** so the builder's inputs match: if `previousSession` is null, do NOT run the two action-item queries — pass empty lists. The builder still applies the "both empty ⇒ null section" rule as a second guard. Net behavior identical.

**Accept:**
- Both endpoints: auth preamble unchanged (diff shows only the post-gate body changed); body is load + `reportBuilder.Build(...)` + `.Adapt<ReportResponse>()` + `Send.OkAsync`.
- The duplicated `BuildColumnSection` / `BuildPreviousActionsSectionAsync` private methods are gone from both endpoint files.
- Both endpoints inject `RetroReportBuilder`.
- The grep for the three `Adapt<Report*Entry>` call-sites returns zero; the three now-dead `Card → ReportCardEntry` / `Card → ReportActionItemEntry` / `EvidenceLink → ReportEvidenceLinkEntry` maps are deleted from `ReportMappings.cs`; only the `RetroReportData → ReportResponse` tree remains.
- `GET /api/teams/{teamId}/retro-sessions/{retroSessionId}/report` and `GET /api/retro-sessions/{retroSessionId}/report` return byte-identical JSON to before.
- `dotnet build SprintRituals.slnx` green.

**Satisfies:** ADR-005 (computation out of the API) + ADR-003 (the duplication — the problem class — is eliminated at the root).

### Step 5 — Register `RetroReportBuilder` in DI

**Disposition: Follow `service-registration`.** **Confidence: high** — one line, mirroring the live `GetSprintContextAssembler` registration.

Modify `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`: add `services.AddScoped<Reflekt.Domain.Reports.RetroReportBuilder>();` in the "Feature services" block (next to `AddScoped<...GetSprintContextAssembler>()`).

**Accept:**
- `AddReflektModule` registers `RetroReportBuilder` (scoped). Both endpoints resolve it. `dotnet build` green.

**Satisfies:** ADR-014/service-registration — the domain service is wired in the owning module's DI.

### Step 6 — Unit tests for `RetroReportBuilder`

**Disposition: Follow `tdd`.** **Confidence: high** — the builder is pure; inputs are hand-built aggregates, no DB.

Create `src/Services/Reflekt/Reflekt.Domain.Tests/Reports/RetroReportBuilderTests.cs`. Build `RetroSession`/`Card`/`CardGroup`/`Participant`/`Team` inputs via their factories (see `RetroSessionCreateTests`/`ParticipantJoinTests` for construction patterns) and assert:
- **Group filter:** a group with `votes <= 1` and not discussed is excluded; a discussed group with 0 votes is included; a group with `votes > 1` is included.
- **Ordering:** column sections list groups in vote-descending order.
- **Title fallback:** a group with null `Title` takes its first (by `CreatedAt`) Note card's text; with no cards → `""`.
- **Column routing:** groups land in the section matching their `ColumnType` (`Column1`/`2`/`3`), and the section carries the RR-B VO `Name`/`Color.Value`.
- **Action items:** `CardType.ActionItem` cards appear in `ActionItems` with assignee + evidence links; Notes do not.
- **Evidence-link projection fidelity:** a card with **multiple** evidence links yields `ReportEvidenceLinkData` entries in the **same list order** as `card.EvidenceLinks`; each entry carries `Url`, the `EvidenceLinkSource` **enum** value, `Title`, `ExternalKey` — and the test asserts the dropped fields are not surfaced (i.e. the record has no `Description`/`IconUrl`/`ImageUrl`/`FetchStatus`/`FetchedAt` member — a compile-time guarantee from the record shape, plus a value assertion on the four kept fields). This pins against silent drift in the `evidenceLinks` array (critic LOW-1).
- **Previous actions:** with `previousSession` null → `PreviousActionsReview` is null; with a previous session but both lists empty → null; with a reviewed item → entry carries `ReviewStatus.ToString()`; with an unreviewed-eligible item → entry carries the literal `"Not reviewed"`.

**Accept:**
- The builder tests cover the filter, ordering, title fallback, column routing, action-item projection, evidence-link enum, and all four previous-actions branches.
- All `Reflekt.Domain.Tests` green.

**Satisfies:** ADR-005 — the lifted computation is now directly unit-tested (it was untestable inline in the endpoint before).

### Step 7 — Update `Reflekt/CLAUDE.md`

**Disposition: None** (doc). **Confidence: high.**

In `src/Services/Reflekt/CLAUDE.md` "Key patterns" (and/or "Domain model"), note: report aggregation lives in `Reflekt.Domain/Reports/RetroReportBuilder` (ADR-005) — a pure domain service; the endpoints load aggregates, call it, and Mapster-map `RetroReportData → ReportResponse` at the boundary (ADR-010). Note the duplication between the two report endpoints was removed by the extraction. Do not invent unrelated doc edits.

**Accept:**
- `Reflekt/CLAUDE.md` records the ADR-005 report-service pattern and the de-duplication.

**Satisfies:** ADR-003 problem-class — the fix and its scope are recorded.

### Step 8 — Verification sweep + build + tests

**Disposition: None** (verification). **Confidence: high.** Objective done-gate.

1. `RetroReportBuilder` exists under `Reflekt.Domain/Reports/`, is pure (grep its file for `DbContext`/`Repository`/`Adapt`/`CancellationToken` → **zero** hits), and returns `RetroReportData`.
2. `RetroReportData.cs` references no `Reflekt.API.*` or `*.Contracts` type (grep → zero); `Reflekt.Domain.csproj` still references only `Blocks.Domain` (ADR-006 intact).
3. Neither report endpoint still declares a private `BuildColumnSection` or `BuildPreviousActionsSectionAsync` (grep both endpoint files → zero); both inject `RetroReportBuilder` and call `.Adapt<ReportResponse>()`.
4. `ReportContracts.cs` is diff-unchanged (the 11 wire records intact). `ReportMappings.cs` registers the `RetroReportData → ReportResponse` tree and **no longer** registers `Card → ReportCardEntry` / `Card → ReportActionItemEntry` / `EvidenceLink → ReportEvidenceLinkEntry` (the three dead maps removed in Step 4); grep `Adapt<ReportCardEntry>`/`Adapt<ReportActionItemEntry>`/`Adapt<ReportEvidenceLinkEntry>` across `src/Services/Reflekt` → zero hits.
5. `dotnet build SprintRituals.slnx` → green (filter `error CS`; ignore MSB file-lock warnings per `project-rules`).
6. `dotnet test` for `Reflekt.Domain.Tests` → green, including the new `RetroReportBuilderTests`.
7. Manual smoke (user, post-pass): `GET /api/teams/{teamId}/retro-sessions/{id}/report` and `GET /api/retro-sessions/{id}/report` return the same JSON as before for a retro with grouped cards, votes, action items, and a previous closed retro with reviewed + unreviewed action items.

**Accept:** all checks pass; build + tests green; `implementation.md` records each grep's actual output and a before/after JSON spot-check for one report.

## Migration Notes

- **No migration.** No EF schema change, no `reflekt.db` change. Package set unchanged (Mapster + `Blocks.Domain` already present). `dotnet build` is the only restore.
- **No repository change.** The builder takes loaded results; the existing repo methods already return exactly what the handlers loaded. No new query method, no repo dependency in the domain.

## Testing Strategy

Reflekt has live test projects; this pass adds to `Reflekt.Domain.Tests` (Step 6). Verification:
1. **Tests** — `RetroReportBuilderTests` (pure, no DB) cover the lifted logic that was previously untestable inline. This is the primary correctness check — it pins the filter/order/fallback/previous-actions behavior the refactor must preserve.
2. **Grep-checkable acceptance** (Step 8 battery: purity grep, no-DTO-in-domain grep, helper-deleted grep, contract diff).
3. **Build** — `dotnet build SprintRituals.slnx` green (the cut-over in Step 4 is the only point the build could break; Steps 1–3 are additive and stay green).
4. **Wire-stability** — `ReportContracts.cs` diff-unchanged; both report endpoints return identical JSON.
5. **Manual smoke (user, post-pass)** — both report endpoints over a populated retro (grouped/voted cards, action items, a prior closed retro with reviewed + unreviewed actions) return the same payload as before.

## KB Impact

None. `docs/kb/` exists but this pass changes no business rule, formula, or edge case — it relocates report aggregation API→Domain and removes a duplication, preserving every filter, order, fallback, and literal. No KB entry warranted.

## Open Questions

None blocking — triage O1 is Resolved (2026-06-20: targeted three-pass scope, RR-C = Reports computation→domain). Developer-time judgments flagged inline (not blockers):

- **Step 1 — one file vs several:** the result-record tree may be one `RetroReportData.cs` or split per concern; developer's call. The Fokus precedent (`SprintSummaryResult.cs`) keeps the whole tree in one file — recommend the same.
- **Step 2 — builder method name:** `Build` vs `Compute` vs `Assemble`. `analytics-computation-service` guidance is "name by what it does"; `Build` reads well for report assembly. Developer's call; any of the three is fine.
- ~~**Step 3 — pruning the old maps**~~ **Resolved (critic LOW-2): the three old maps are dead after Step 4 and their removal is mandatory, not optional.** The four `Adapt<Report*Entry>` call-sites are all inside the two endpoints; Step 4 deletes them, leaving the `Card → ReportCardEntry` / `Card → ReportActionItemEntry` / `EvidenceLink → ReportEvidenceLinkEntry` configs with zero consumers. The Step 4 pruning sub-step deletes all three (grep confirms zero callers). Not a judgment call.

**Flags for the team-lead (not blockers):**
1. **Binding-doc reference gap.** `.claude/rules/engineering-discipline.md` (cited in the task brief + `docs/backlog.md:3`) does not exist in this repo; the real guardrails doc is `docs/conventions/project-rules.md`. Reconcile the reference. The ADRs this plan is gated on are intact.
2. **Two-file de-duplication is a happy by-product.** The ADR-005 lift also removes ~80 lines of duplicated aggregation across the two endpoints. That is the ADR-003 problem-class root-cause fix (not just the symptom), not separate scope — called out so the reviewer credits it as intended, not accidental.
