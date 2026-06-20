# RR-B — Reflekt Rich Value Objects (ADR-007)

**Feature Spec:** None (adhoc refactoring pass — scope in `docs/backlog.md:118` row RR-B + `docs/proposals/reflekt-refactor-triage.md` RR-B/RR-E rows; the gate is the **user-point register ADR-007** via the triage's problem-class framing **ADR-003**. No open questions — triage O1/O2/O3 are Resolved 2026-06-20.)

## Context

ADR-007 (rich aggregates / value objects), applied as a **problem class** per ADR-003: group flat cohesive scalar clusters into value objects across **all** Reflekt aggregates. The Phase-1 scan (architect) confirmed the problem class has exactly **one** instance carrying real debt — the `RetroSession` aggregate — and **zero** instances on the other five aggregates (Card, CardGroup, Participant, Vote, Team are thin: FK + incidental scalar + timestamp, no cohesive cluster). So this pass refactors `RetroSession` and records the scan that clears the rest.

`RetroSession` (`src/Services/Reflekt/Reflekt.Domain/RetroSessions/RetroSession.cs`) carries two flat clusters:

1. **Column triple** → a `RetroColumn` VO (`Name` + `Color`). Today: 6 flat props — `Column{1,2,3}Name` (string) + `Column{1,2,3}Color` (already the `ColumnColor` VO). The Name+Color pair is one concept (a board column) and is always set/read together. Group into 3× `RetroColumn { string Name, ColumnColor Color }`.
2. **Timer triple** → a `TimerDefaults` VO (`Writing` + `Voting` + `Discussing`). Today: 3 flat `required int` — `Writing/Voting/DiscussingTimerDefault`, each constrained to 1–60. The 1–60 validation (`ValidateTimerRange`) and `GetTimerDefaultForPhase()` phase-switch are the VO's natural behavior.

The VOs are **domain-internal**. The wire (the two response contracts, the two commands) **stays byte-for-byte flat** — mapped flat↔grouped at the endpoint boundary (Mapster on the read side; flat factory/behavior params on the write side). EF persists the new VOs as **flat columns via `ComplexProperty`** (not JSON), via a **column-rename** migration that preserves existing data.

This mirrors the Fokus precedent exactly: `adhoc-Pass3b-AppSettingsVOs` / ADR-007 Q1=A — flat scalars → `ComplexProperty` VOs, wire stays flat, rename-not-drop migration (`20260607075717_GroupAppSettingsScalarsIntoVOs`). **Differences from that pass that change the work:** (a) `RetroSession` is a real aggregate mutated through behavior methods + a factory (not a plain settings class with public setters), and (b) `RetroSessionRepository` uses **tracked in-place mutation** + `SaveChangesAsync` (it does NOT use `CurrentValues.SetValues`/`Adapt`-onto-existing), so the Pass-3b "L5 SetValues does not deep-copy complex VOs" write-funnel hazard **does not apply here** — verified in Phase 1, recorded under Migration Notes, not a step.

The pass also folds the trivial **RR-E N1** cleanup: delete the empty `Sprints/{Behaviors,Events}` leftover dirs from the RA3 `Sprint`→`RetroSession` rename.

## Scope

**In scope:**
1. Create `RetroColumn` + `TimerDefaults` VOs in `Reflekt.Domain/RetroSessions/` (extending `Blocks.Domain.ValueObjects.ValueObject`); regroup the 9 flat props on `RetroSession` into 3× `RetroColumn` + 1× `TimerDefaults`.
2. Move the column-name and timer-range validation + `GetTimerDefaultForPhase()` into the VOs; rewire the `Create` factory and `UpdateColumnConfig` behavior to construct/replace VOs (their flat parameter signatures are unchanged).
3. EF `ComplexProperty` config for both VOs in `RetroSessionConfiguration.cs` + one Reflekt migration (column **rename**, defaults + max-lengths preserved).
4. Repoint the **six** internal read sites that consume the flat props (`RetroSessionMappings` Mapster config, `GetRetroSummary` manual construction, `CreateRetro` previous-session copy, the two Report endpoints' `BuildColumnSection` calls, and the `BroadcastColumnConfigUpdated` SignalR-payload handler) to the grouped paths — keeping every wire DTO **and the SignalR `ColumnConfigUpdated` payload** byte-flat.
5. New domain tests for the two VOs (construction, validation, equality) + a persistence round-trip test asserting columns/timers survive save→reload.
6. Delete the empty `Reflekt.Domain/Sprints/` leftover dirs (RR-E N1).
7. Update `src/Services/Reflekt/CLAUDE.md` domain-model VO note.
8. Verification sweep + build + tests.

**Explicitly out of scope (do NOT touch):**
- **The other five aggregates** (`Card`, `CardGroup`, `Participant`, `Vote`, `Team`) and `EvidenceLink` / `FokusSprintSnapshot` / `FokusSprintRef` — Phase 1 confirmed no VO cluster. Do NOT invent VOs there. (Recording the scan in CLAUDE.md is the only deliverable touching them.)
- **The wire contract.** `RetroSessionResponse`, `RetroSessionSummaryResponse`, `CreateRetroSessionCommand`, `UpdateColumnsCommand` (+ their validators) keep the **flat** 9-field shape (`Column1Name`, `Column1Color`, … `WritingTimerDefault`, …). No client/Vue change. The Reflekt SPA is not in scope.
- **The SignalR `ColumnConfigUpdated` payload shape.** `BroadcastColumnConfigUpdatedHandler` emits an anonymous object with the camelCase keys `column1Name`/`column1Color`/…/`column3Color` — those **key names stay byte-identical** (the client deserializes them). Only the *source* of each value repoints to the grouped path (Step 4). This is a read-side repoint, not a contract change.
- **`ColumnColor`** — already a correct hand-rolled value type (value equality, `Create`/`IsValid` palette guard). Keep it as-is; `RetroColumn` **composes** it. Re-basing `ColumnColor` onto `Blocks.Domain.ValueObjects.ValueObject` is an optional boy-scout, NOT required by this pass (see Open Questions).
- **`AppSettingsRepository`-style `SetValues` re-assignment block** — `RetroSessionRepository` has no such path (it mutates tracked instances). Do NOT add one.
- **Any behavior, formula, validation rule, error shape, or default value change.** Pure structural refactor: same column names on the wire, same defaults ("Went Well"/green/5/3/10…), same 1–60 timer guard, same 30-char name guard, same palette guard. No observable behavior difference.
- **RR-C** (Reports computation→domain) and **RR-D** (event-correctness) — not pre-empted.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | `domain-patterns` (Value Objects + "When to Use a VO vs a Flat Scalar") | Follow | yes | The 2 VO names + members/types/defaults; place in `Reflekt.Domain/RetroSessions/`; `RetroColumn` composes existing `ColumnColor`; move `ValidateColumnName`/`ValidateTimerRange`/`GetTimerDefaultForPhase` into the VOs | — |
| 2 | `domain-patterns` (Partial Class Behavior Split) | Follow | yes | Rewire `RetroSession.Create` + `UpdateColumnConfig` to build/replace VOs; flat param signatures unchanged; `RetroSession.cs` (state) holds 3×`RetroColumn` + 1×`TimerDefaults` | — |
| 3 | `persistence-patterns` (Value Object Mapping → `ComplexProperty`) | Follow | no | Map `RetroColumn` (×3) + `TimerDefaults` as `ComplexProperty`; preserve column names via rename migration; max-lengths (Name 30, Color 10) + defaults | — |
| 4 | (none) | — | yes | Repoint 6 internal read sites (RetroSessionMappings, GetRetroSummary, CreateRetro copy, 2 Report endpoints, BroadcastColumnConfigUpdated SignalR handler) to grouped paths; wire DTOs + SignalR payload keys stay flat | Log to lessons (boundary flatten/unflatten read-side map — same family as Pass-3b Step 4 gap) |
| 5 | `tdd` | Follow | yes | VO construction/validation/equality tests (Domain.Tests); column/timer save→reload round-trip test (Persistence.Tests) | — |
| 6 | (none) | — | no | Delete empty `Reflekt.Domain/Sprints/{Behaviors,Events}` dirs (RR-E N1) | — |
| 7 | (none) | — | no | `Reflekt/CLAUDE.md` domain-model VO note (RetroSession VOs + scan-clears-the-rest) | — |
| 8 | (none) | — | no | Verification sweep + build + test run | — |

**Disposition notes:**
- **Step 1 Follow `domain-patterns`** — the skill's "When to Use a Value Object vs a Flat Scalar (ADR-007)" section is the binding rule; its VO base shape (`ValueObject` + `GetEqualityComponents()`, static `Create()` + internal/private ctor with validation) is the template. The plan supplies only the two new VO names + members; the developer invokes the skill for the structural shape. `ColumnColor` is the in-repo precedent for a single-value guard VO — but `RetroColumn`/`TimerDefaults` are multi-property, so they extend `ValueObject`, not `ColumnColor`'s hand-rolled shape.
- **Step 2 Follow `domain-patterns`** (Partial Class Behavior Split) — `RetroSession` already uses the state/`Behaviors` split; this step keeps the validation+events in `Behaviors/RetroSession.cs` and moves only the *value-level* guards into the VOs. The plan supplies the rewire surface; the skill supplies the split convention.
- **Step 3 Follow `persistence-patterns`** (Value Object Mapping) — the skill covers `ComplexProperty` vs `OwnsOne`. The plan supplies which VOs map as `ComplexProperty` and the rename/default/max-length constraints; the developer invokes the skill for the config pattern and reads `docs/conventions/ef-core.md` for migration-default parity.
- **Step 4 None** — repointing internal read sites + Mapster flatten config across the boundary has no dedicated skill (same family as the Pass-3b Step 4 boundary-map gap). Logged.
- **Step 5 Follow `tdd`** — testable behavior on the VOs and on persistence; red-green per the `tdd` skill.
- **Steps 6, 7, 8 None** — a dir delete, a doc edit, and the verification battery. No skill.
- **No "Build."** All infrastructure exists: `Blocks.Domain.ValueObjects.ValueObject` base, Mapster (`RetroSessionMappings` is a live `IRegister`), `ComplexProperty` (used in Fokus), the two test projects.

## Domain Model Changes

Two new value objects, each in its own file under `src/Services/Reflekt/Reflekt.Domain/RetroSessions/`, extending `Blocks.Domain.ValueObjects.ValueObject`:

| New VO | Members (name : type) | Validation moved in | Defaults (preserved) |
|--------|----------------------|---------------------|----------------------|
| `RetroColumn` | `Name : string`, `Color : ColumnColor` | `ValidateColumnName` (non-empty, ≤30 chars, trimmed) — currently `Behaviors/RetroSession.cs:103` | per-column: `("Went Well", green)`, `("Didn't Go Well", red)`, `("Start Doing", amber)` |
| `TimerDefaults` | `Writing : int`, `Voting : int`, `Discussing : int` | `ValidateTimerRange` (1–60) — currently `Behaviors/RetroSession.cs:82`; plus `GetTimerDefaultForPhase(RetroPhase)` moves onto the VO | `Writing=5, Voting=3, Discussing=10` |

**VO shape (follow `domain-patterns`):** static `Create(...)` factory performing the validation, private/internal ctor, `GetEqualityComponents()` yielding every member. `RetroColumn.Color` is a `ColumnColor` (compose, don't duplicate the palette guard). `RetroColumn`'s equality component for `Color` is the `ColumnColor` instance (value-equal). Provide a parameterless private ctor only if EF complex-type materialization needs it (Step 3 acceptance proves it).

**`RetroSession` state (`RetroSession.cs`) after regrouping holds:**
- Unchanged top-level: `TeamId`, `Name`, `State`, `CurrentPhase`, `VotesPerParticipant`, `CreatedAt`, `ClosedAt`, `FocusedGroupId`, `_fokusSprintRefs`/`FokusSprintRefs`.
- Replaced: the 6 column props → `public RetroColumn Column1 { get; private set; }`, `Column2`, `Column3` (each initialized to its default via `RetroColumn.Create(...)`).
- Replaced: the 3 timer props → `public TimerDefaults Timers { get; private set; }` (initialized to defaults).

> **`required init` → VO interaction (binding — pre-empts a CS9032 / init detour):** the 3 timer props are currently `required ... { get; init; }`, set in the `Create` object-initializer. After grouping into `Timers`, the timer values arrive through the factory's `TimerDefaults.Create(...)` call, not an object-initializer `required` member. Per `domain-patterns` → Entity note, do **not** combine `required` with a non-public setter. Make `Timers` a plain `{ get; private set; }` assigned inside `Create` (the factory is already the sole construction path; `required` is redundant once the factory builds the VO). Same for the 3 `Column{N}` members — `private set`, assigned in `Create`. The column members are already `private set` today, so only the timer trio changes accessor kind.

**The flat-wire ↔ grouped-domain name mapping the developer must preserve (read side, Mapster + manual construction):**

| Flat wire name (unchanged) | Grouped domain path |
|----------------------------|---------------------|
| `Column1Name` | `Column1.Name` |
| `Column1Color` | `Column1.Color.Value` |
| `Column2Name` | `Column2.Name` |
| `Column2Color` | `Column2.Color.Value` |
| `Column3Name` | `Column3.Name` |
| `Column3Color` | `Column3.Color.Value` |
| `WritingTimerDefault` | `Timers.Writing` |
| `VotingTimerDefault` | `Timers.Voting` |
| `DiscussingTimerDefault` | `Timers.Discussing` |

## Data Model Changes

**reflekt.db:** One migration. The 9 flat `RetroSessions` columns are restructured into `ComplexProperty` columns. With `ComplexProperty`, EF Core names flat columns `{ComplexProp}_{Member}` by default — so `Column1Name` → `Column1_Name`, `Column1Color` → `Column1_Color`, `WritingTimerDefault` → `Timers_Writing`, etc. **Because the column names change, the migration must be a column RENAME, not drop+add**, to preserve existing retro-session rows (column names, custom palette colors, custom timers). Verify each new column keeps its existing default and max-length:
- `Column{N}_Name`: `IsRequired`, `HasMaxLength(30)`, defaults `"Went Well"`/`"Didn't Go Well"`/`"Start Doing"`.
- `Column{N}_Color`: `IsRequired`, `HasMaxLength(10)`, defaults `green`/`red`/`amber`, `HasConversion(c => c.Value, s => ColumnColor.Create(s))` — the conversion now lives inside the `ComplexProperty` builder for the `Color` member.
- `Timers_{Writing,Voting,Discussing}`: `IsRequired`, defaults `5`/`3`/`10`.

The `FokusSprintRefs` child relationship, `TeamId` FK/index, `ClosedAt` index, and all other columns are unchanged.

> **SQLite rename caveat:** SQLite has limited `ALTER TABLE` support; EF's SQLite provider implements `RenameColumn` via table-rebuild under the hood, which is fine, but confirm the generated migration emits `RenameColumn` (or an equivalent rebuild that preserves data) and not `DropColumn`+`AddColumn`. If EF emits drop+add, hand-edit to `migrationBuilder.RenameColumn(...)` per the name map above. Reference the Fokus `20260607075717_GroupAppSettingsScalarsIntoVOs` migration for the rename approach.

## Implementation Steps

**Ordering (binding):** Steps 1→2→3 are one atomic domain+persistence change — the solution will be RED between them (regrouping breaks the factory, the EF config, and the 6 read sites simultaneously). Build-green is expected only after Step 4 lands. Step 5 (tests) follows green. Steps 6 (dir delete) and 7 (docs) any time. Step 8 verifies last.

### Step 1 — Create `RetroColumn` + `TimerDefaults` VOs; regroup `RetroSession` state

**Disposition: Follow `domain-patterns`** (Value Objects + the VO-vs-flat-scalar rule). **Confidence: high** — `ColumnColor` and the `ValueObject` base are the live in-repo templates.

Create the two VOs (members/validation/defaults in **Domain Model Changes**) and regroup `RetroSession.cs` state. Move `ValidateColumnName`, `ValidateTimerRange`, and `GetTimerDefaultForPhase` out of `Behaviors/RetroSession.cs` into the VOs.

- **Create** `src/Services/Reflekt/Reflekt.Domain/RetroSessions/RetroColumn.cs` — extends `ValueObject`; `Create(string name, ColumnColor color)` (or `Create(string name, string color)` constructing `ColumnColor` internally — developer's call, but the factory must run the ≤30-char/non-empty name guard); `GetEqualityComponents()` yields `Name` + `Color`.
- **Create** `src/Services/Reflekt/Reflekt.Domain/RetroSessions/TimerDefaults.cs` — extends `ValueObject`; `Create(int writing, int voting, int discussing)` runs the 1–60 guard per member; carries `GetForPhase(RetroPhase)` (the moved `GetTimerDefaultForPhase` switch); `GetEqualityComponents()` yields all three.
- **Modify** `src/Services/Reflekt/Reflekt.Domain/RetroSessions/RetroSession.cs` — replace the 6 column props with `Column1/2/3 : RetroColumn { get; private set; }` and the 3 timer props with `Timers : TimerDefaults { get; private set; }`; initialize each to its default. Remove the now-VO-owned `required` on the timer trio (see the binding note in Domain Model Changes).

**Accept:**
- Two VO files exist, each extends `ValueObject`, has a validating `Create(...)`, and a `GetEqualityComponents()` covering every member. `RetroColumn` composes `ColumnColor` (no duplicated palette list).
- `RetroSession.cs` declares `Column1/2/3` (`RetroColumn`) + `Timers` (`TimerDefaults`) and no longer declares any of the 9 flat props. No `required` + non-public-setter pairing (no CS9032).
- The timer 1–60 guard and column ≤30-char guard now live in the VO `Create`; `GetTimerDefaultForPhase` is gone from `RetroSession` (moved to `TimerDefaults.GetForPhase`).

**Satisfies:** ADR-007 (rich VOs) / ADR-003 problem-class — the confirmed `RetroSession` instance.

### Step 2 — Rewire the `Create` factory + `UpdateColumnConfig` behavior to build/replace VOs

**Disposition: Follow `domain-patterns`** (Partial Class Behavior Split). **Confidence: high.**

Keep the flat parameter signatures of `RetroSession.Create(...)` and `UpdateColumnConfig(...)` **unchanged** (their callers and the wire are flat). Inside, construct/replace the VOs instead of assigning flat props.

- **Modify** `src/Services/Reflekt/Reflekt.Domain/RetroSessions/Behaviors/RetroSession.cs`:
  - `Create(...)` — build `Timers = TimerDefaults.Create(writing ?? 5, voting ?? 3, discussing ?? 10)` and, for each column, `Column{N} = RetroColumn.Create(name ?? default, color ?? default)`. The per-arg null→default fallback and the "explicit value validated" behavior are preserved (now enforced inside the VO `Create`). `RetroSessionCreatedEvent(session)` still raised.
  - `UpdateColumnConfig(...)` — replace the 6 flat assignments with `Column1 = RetroColumn.Create(column1Name, column1Color)` (×3); still raise `ColumnConfigUpdatedEvent(this)`.
  - `GetTimerDefaultForPhase()` — delegate to `Timers.GetForPhase(CurrentPhase)` (or remove and have callers use `Timers.GetForPhase` — check callers via grep; keep a thin pass-through if any external caller exists).
- Phase/state/focus behaviors (`Start`, `AdvancePhase`, `Close`, `SetFocus`, `ClearFocus`) are untouched.

**Accept:**
- `Create` and `UpdateColumnConfig` keep identical flat parameter lists; internals build VOs.
- Same defaults and same validation outcomes as before (a 0-length name still throws; a timer of 0 or 61 still throws; an invalid color still throws via `ColumnColor.Create`).
- `RetroSessionCreatedEvent` / `ColumnConfigUpdatedEvent` still raised at the same points.

**Satisfies:** ADR-007 — behavior preserved through the VO refactor.

### Step 3 — EF `ComplexProperty` config + Reflekt migration (rename, not drop)

**Disposition: Follow `persistence-patterns`** (Value Object Mapping → `ComplexProperty`). **Confidence: medium** — the rename-not-drop migration (data preservation) and moving the `ColumnColor` `HasConversion` inside the complex-type builder are the delicate points.

Map `Column1/2/3` and `Timers` as `ComplexProperty` in `RetroSessionConfiguration.cs`; carry over every `IsRequired`/`HasMaxLength`/`HasDefaultValue`/`HasConversion` from the current flat config (full list in Data Model Changes). Generate one migration and ensure it renames the 9 columns.

- **Modify** `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/RetroSessionConfiguration.cs` — replace the 9 flat `builder.Property(s => s.Column{N}Name/Color/...)` lines with `builder.ComplexProperty(s => s.Column1, b => { b.Property(c => c.Name)...; b.Property(c => c.Color).HasMaxLength(10).HasConversion(...); })` (×3) and `builder.ComplexProperty(s => s.Timers, b => { b.Property(t => t.Writing).HasDefaultValue(5); ... })`. The `ColumnColor` `HasConversion` moves onto the `Color` member inside the complex builder.
- **EF complex-type non-null rule (binding):** EF requires the complex *instance* to be non-null — the `= ...Create(default)` initializers on `Column1/2/3`/`Timers` (Step 1) satisfy this. Do NOT mark a `ComplexProperty` optional. (All members here are non-nullable scalars/`ColumnColor`.)
- **Create** the migration: `dotnet ef migrations add GroupRetroSessionColumnsAndTimersIntoVOs --project src/Services/Reflekt/Reflekt.Persistence --startup-project src/Host` (use the `DesignTimeDbContextFactory` if startup fails, per `docs/conventions/ef-core.md`).
- **Verify/edit** the generated migration to RENAME the 9 columns (`Column1Name`→`Column1_Name`, `Column1Color`→`Column1_Color`, … `WritingTimerDefault`→`Timers_Writing`, …) rather than drop+add. Confirm defaults and max-lengths match the config per `ef-core.md` "Migration Defaults".

**Accept:**
- `RetroSessionConfiguration.cs` maps `Column1/2/3` + `Timers` via `ComplexProperty`; the `ColumnColor` conversion is on the `Color` member; max-lengths (Name 30, Color 10) and the 6 defaults are preserved.
- The migration renames (not drops) the 9 columns; `ReflektDbContextModelSnapshot.cs` reflects the `_`-separated complex columns; column defaults match the config.
- `dotnet build SprintRituals.slnx` green (after Step 4 also lands — see ordering).

**Satisfies:** ADR-007 — VOs persisted as flat columns, data-preserving.

### Step 4 — Repoint the 6 internal read sites to grouped paths (wire + SignalR payload stay flat)

**Disposition: None** (boundary flatten read-map; no skill). **Confidence: high** — all six sites enumerated from a grep of the aggregate-property accesses; the rename target is the grouped path from the Domain Model Changes table.

Keep all four wire DTOs **and the SignalR payload keys** flat. Repoint the six places that read the (now-grouped) props:

- **`RetroSessionMappings`** (`src/Services/Reflekt/Reflekt.API/Mappings/RetroSessionMappings.cs`) — the `RetroSession → RetroSessionResponse` Mapster config currently maps `d.Column1Color ← s.Column1Color.Value`. Repoint all 9 column/timer fields to the grouped paths: `d.Column1Name ← s.Column1.Name`, `d.Column1Color ← s.Column1.Color.Value`, … `d.WritingTimerDefault ← s.Timers.Writing`, etc. Because the flat dest names no longer line up with the grouped source paths by Mapster's flattening convention, add an **explicit `.Map(...)` rule for each of the 9 fields**. (`State`/`CurrentPhase` `.ToString()` maps are unchanged.)
- **`GetRetroSummaryEndpoint`** (`.../Features/RetroSessions/GetRetroSummary/GetRetroSummaryEndpoint.cs:44-52`) — the manual `RetroSessionSummaryResponse(...)` construction reads `session.Column1Name`, `session.Column1Color.Value`, `session.WritingTimerDefault`, … — repoint to `session.Column1.Name`, `session.Column1.Color.Value`, `session.Timers.Writing`, … The response shape and the `isScrumMaster`/participants logic are unchanged.
- **`CreateRetroSessionEndpoint`** (`.../Features/RetroSessions/CreateRetro/CreateRetroEndpoint.cs:42-47`) — the "copy column config from most recent session" block reads `mostRecent.Column1Name`, `mostRecent.Column1Color.Value`, … to populate the flat local `col{N}Name`/`col{N}Color` strings passed to the factory. Repoint to `mostRecent.Column1.Name`, `mostRecent.Column1.Color.Value`, … The factory call (flat params) is unchanged.
- **`GetRetroReportEndpoint`** (`.../Features/Reports/GetRetroReport/GetRetroReportEndpoint.cs:75-77`) — the `columnSections` list passes `session.Column1Name`, `session.Column1Color.Value` (×3) into `BuildColumnSection(...)`. Repoint to `session.Column1.Name`, `session.Column1.Color.Value`, … The `BuildColumnSection` signature (flat `string name, string color`) and the report response shape are unchanged.
- **`GetParticipantReportEndpoint`** (`.../Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs:75-77`) — identical `BuildColumnSection` call shape as the retro report; repoint the same three `session.Column{N}Name`/`.Color.Value` reads.
- **`BroadcastColumnConfigUpdatedHandler`** (`.../Features/RetroSessions/UpdateColumns/BroadcastColumnConfigUpdatedHandler.cs:15-20`) — the SignalR `ColumnConfigUpdated` anonymous payload reads `ev.RetroSession.Column1Name`, `ev.RetroSession.Column1Color.Value`, … into the camelCase keys `column1Name`/`column1Color`/… Repoint each value source to `ev.RetroSession.Column1.Name`, `ev.RetroSession.Column1.Color.Value`, … **The payload key names (`column1Name`, etc.) MUST NOT change** — the client deserializes them; only the right-hand value source moves to the grouped path.

**Verification sub-step (binding — grep before declaring done):** after the rewrite,
`rg -n "\.(Column1Name|Column2Name|Column3Name|Column1Color|Column2Color|Column3Color|WritingTimerDefault|VotingTimerDefault|DiscussingTimerDefault)\b" src/Services/Reflekt --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*' -g '!*/Migrations/*'`
must return **only** hits that are flat **wire-DTO field declarations/uses** — the `RetroSessionResponse`/`RetroSessionSummaryResponse` record members, the `CreateRetroSessionCommand`/`UpdateColumnsCommand` record members, and their FluentValidation `RuleFor(x => x.Column1Name)` lines (those stay flat). The SignalR payload keys (`column1Name`/`column1Color`/…) are **lowercase** and will NOT match this PascalCase-anchored pattern — they are intentionally excluded and stay as-is. **Zero** hits may be a `RetroSession`-aggregate property access (`session.Column1Name`, `mostRecent.Column1Color`, `ev.RetroSession.Column1Name`, etc.). Migrations are excluded (they reference old column names by design via the rename). If any aggregate access remains flat, it's a missed site — fix before proceeding.

**Accept:**
- `RetroSessionResponse`, `RetroSessionSummaryResponse`, `CreateRetroSessionCommand`, `UpdateColumnsCommand` (+ validators) are structurally unchanged (flat 9 fields; verify via diff — no field renamed/removed).
- The `BroadcastColumnConfigUpdatedHandler` payload **keys** (`column1Name`…`column3Color`) are unchanged (verify via diff — only the value-source expressions moved to grouped paths).
- The grep above returns zero aggregate-property hits across all six sites (Mappings, GetRetroSummary, CreateRetro, both Report endpoints, the Broadcast handler).
- `RetroSessionMappings` has an explicit `.Map` for each of the 9 flat fields; a `GET …/summary`, a `POST` create, and a `GET …/report` return the same flat field names + values as before.
- `dotnet build SprintRituals.slnx` green (Reflekt compiles end-to-end).

**Satisfies:** ADR-007 — wire-flat boundary preserved (mirrors Pass-3b Q1=A).

### Step 5 — Tests: VO behavior + persistence round-trip

**Disposition: Follow `tdd`.** **Confidence: high.**

- **Create** `src/Services/Reflekt/Reflekt.Domain.Tests/RetroSessions/RetroColumnTests.cs` and `TimerDefaultsTests.cs` (or one VO-tests file) — cover: valid construction; name >30 chars / empty throws; timer <1 or >60 throws; invalid color throws (via `ColumnColor`); value equality (two VOs with same members are `Equal`, differ → not equal); `TimerDefaults.GetForPhase` returns the right member per `RetroPhase` and throws on a non-timer phase (matching the current `GetTimerDefaultForPhase` behavior).
- **Add** to `Reflekt.Domain.Tests/RetroSessions/RetroSessionCreateTests.cs` (or a sibling) — a `Create` with custom columns/timers produces the expected VO values; `UpdateColumnConfig` replaces the column VOs and raises `ColumnConfigUpdatedEvent`.
- **Create** `src/Services/Reflekt/Reflekt.Persistence.Tests/RetroSessions/RetroSessionColumnTimerPersistenceTests.cs` — **two** round-trip cases against a real SQLite context:
  - **Custom-values:** save a `RetroSession` with non-default columns + timers, reload from a fresh context, assert all 9 values survive (proves the `ComplexProperty` mapping + `ColumnColor` conversion round-trip).
  - **Default-row:** save a default-constructed session (no explicit column/timer overrides), reload, assert the defaults survive (`Went Well`/green/5/3/10…). This exercises the `HasDefaultValue` on the **renamed** complex columns — the custom-values case alone would not catch a default that was lost in the rename migration.
  - Follow the existing persistence-test harness shape (`Reflekt.Persistence.Tests/Participants/ParticipantRepositoryTests.cs` / `FokusSprints/FokusSprintSnapshotRepositoryTests.cs` for the in-memory-SQLite context setup).

**Accept:**
- New VO tests cover construction, each validation branch, equality, and `GetForPhase`.
- Both persistence round-trip tests (custom-values + default-row) pass against the real `ComplexProperty` config (not an in-memory provider that skips conversions — use the same SQLite test context the existing persistence tests use).
- All Reflekt tests green.

**Satisfies:** ADR-007 — behavior + persistence parity proven.

### Step 6 — Delete empty `Sprints/` leftover dirs (RR-E N1)

**Disposition: None** (dir cleanup). **Confidence: high** — Phase 1 confirmed both dirs contain zero files.

Delete `src/Services/Reflekt/Reflekt.Domain/Sprints/Behaviors/` and `src/Services/Reflekt/Reflekt.Domain/Sprints/Events/` (and the now-empty parent `Sprints/`). Leftovers from the RA3 `Sprint`→`RetroSession` rename.

**Accept:**
- `src/Services/Reflekt/Reflekt.Domain/Sprints/` no longer exists.
- `dotnet build` green (nothing referenced them — they were empty).

**Satisfies:** RR-E N1 (triage cleanup arm).

### Step 7 — Update `Reflekt/CLAUDE.md` domain-model VO note

**Disposition: None** (doc). **Confidence: high.**

In `src/Services/Reflekt/CLAUDE.md` "Domain model" section, note that `RetroSession` carries the `RetroColumn` (×3) and `TimerDefaults` value objects (ADR-007), persisted as flat `ComplexProperty` columns with the wire kept flat at the endpoint boundary; and that the VO problem-class scan (RR-B) found no cluster on the other aggregates. Do not invent unrelated doc edits.

**Accept:**
- `Reflekt/CLAUDE.md` names the two new `RetroSession` VOs and the wire-flat boundary rule, and records that the other aggregates were scanned-and-cleared.

**Satisfies:** ADR-003 problem-class — the scan result is recorded, not just the fix.

### Step 8 — Verification sweep + build + tests

**Disposition: None** (verification). **Confidence: high.** Objective done-gate.

1. `rg -n "\.(Column1Name|Column2Name|Column3Name|Column1Color|Column2Color|Column3Color|WritingTimerDefault|VotingTimerDefault|DiscussingTimerDefault)\b" src/Services/Reflekt --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*' -g '!*/Migrations/*'` → only flat **wire-DTO** hits (the 2 response records, the 2 command records, their validators); **zero** `RetroSession`-aggregate property accesses (all six read sites repointed).
2. The two VO files exist under `Reflekt.Domain/RetroSessions/`, extend `ValueObject`, implement `GetEqualityComponents()`. `RetroSession.cs` declares `Column1/2/3` + `Timers` and none of the 9 flat props.
3. `RetroSessionConfiguration.cs` uses `ComplexProperty` for both VOs; exactly one new Reflekt migration; it renames (not drops) the 9 columns; `ReflektDbContextModelSnapshot.cs` shows the `_`-named complex columns.
4. `RetroSessionMappings` has 9 explicit `.Map` rules; the 4 wire DTOs are diff-unchanged (flat); the `BroadcastColumnConfigUpdatedHandler` payload keys (`column1Name`…`column3Color`) are diff-unchanged.
5. `src/Services/Reflekt/Reflekt.Domain/Sprints/` is gone.
6. `dotnet build SprintRituals.slnx` → green (filter `error CS`; ignore MSB file-lock warnings per `project-rules`).
7. `dotnet test` for `Reflekt.Domain.Tests` + `Reflekt.Persistence.Tests` → all green, including the new VO tests and both round-trip tests (custom-values + default-row).

**Accept:** all 7 checks pass; build + tests green; `implementation.md` records each grep's actual output and the migration rename diff.

## Cross-Service Changes

None. No gRPC, integration-event, or wire-contract change. `ISprintAnalyticsService`/`IPersonService` clients and the `SprintSynced` consumer are untouched. The **SignalR `ColumnConfigUpdated` payload is preserved** — Step 4 repoints only the value *source* inside `BroadcastColumnConfigUpdatedHandler` (grouped path) while keeping the camelCase payload keys (`column1Name`…`column3Color`) byte-identical, so the client contract is unchanged. No client/Vue change.

## Migration Notes

- **Reflekt:** one migration (Step 3) — `ComplexProperty` flat columns via **column rename** to preserve existing retro rows. Verify defaults + max-lengths per `docs/conventions/ef-core.md` "Migration Defaults". SQLite `RenameColumn` is a table-rebuild under the hood — confirm the generated op preserves data (hand-edit drop+add → `RenameColumn` if EF emits the wrong shape).
- **No repository write-funnel change.** `RetroSessionRepository` uses tracked in-place mutation (`UpdateColumnConfig` on a loaded entity) + `SaveChangesAsync` — it does NOT use `CurrentValues.SetValues`/`Adapt`-onto-existing, so the Pass-3b "L5: `SetValues` doesn't deep-copy complex VOs" hazard does not apply. The persistence round-trip test (Step 5) is the proof EF tracks the `ComplexProperty` mutation correctly. Do not add an `AppSettingsRepository`-style manual re-assignment block.
- Package set unchanged (`ValueObject` base + Mapster already present). `dotnet build` is the only restore.

## Testing Strategy

The project's existing passes are manually verified, but Reflekt **has** unit/persistence test projects (`Reflekt.Domain.Tests`, `Reflekt.Persistence.Tests`) and this pass adds to them (Step 5). Verification is:
1. **Tests** — new VO behavior tests + the `ComplexProperty` save→reload round-trip (the round-trip is the only check that catches a broken complex-type conversion; a shape grep cannot).
2. **Grep-checkable acceptance** (Step 8 battery).
3. **Build** — `dotnet build SprintRituals.slnx` green (expect RED across Steps 1–3 until Step 4 lands).
4. **Wire-stability:** the 4 wire DTOs diff-unchanged; a create + summary round-trip returns identical flat field names/values.
5. **Manual smoke (user, post-pass):** create a retro with custom columns/timers, reload — values persist; update columns via the SM endpoint, reload — values persist; app starts and `reflekt.db` migrates with existing rows' column/timer values intact.

## KB Impact

None. `docs/kb/` exists but this pass changes no business rule, formula, or edge case — it regroups fields into VOs and preserves every value, default, and guard. No KB entry warranted.

## Open Questions

None blocking — triage O1/O2/O3 are Resolved (2026-06-20). Developer-time judgments flagged inline (not blockers):
- **`ColumnColor` base-class alignment (boy-scout, optional):** `ColumnColor` is a correct hand-rolled value type but does not extend `Blocks.Domain.ValueObjects.ValueObject` (it predates / diverges from the base). Re-basing it onto `ValueObject` (replacing its hand-rolled `Equals`/`GetHashCode` with `GetEqualityComponents`) is a clean consistency win but is **out of scope** for RR-B — do it only as an explicit boy-scout if touching the file anyway, and never at the cost of changing its `Create`/`IsValid`/palette behavior. Default: leave it.
- **Step 1 — `RetroColumn.Create` signature:** `Create(string name, ColumnColor color)` vs `Create(string name, string color)` (constructing `ColumnColor` internally). Developer's call; either is fine as long as the name guard runs and `ColumnColor`'s palette guard is honored.
- **Step 3 — rename vs drop+add:** the migration MUST rename to preserve data. If EF's SQLite provider emits drop+add, hand-edit to `RenameColumn` per the name map (Data Model Changes).
- **Step 3 — EF private ctor for complex types:** the concern is only about the **new VO types** — add a `private RetroColumn() {}` / `TimerDefaults() {}` only if complex-type materialization proves it necessary (the build/round-trip test is the arbiter). `RetroSession` itself already has a `private RetroSession() {}` (RetroSession.cs:6), so the aggregate needs no ctor change.

**Flags for the team-lead (not blockers):**
1. **Skill gap — VO-grouping boundary read-map.** Step 4 (repoint internal reads + Mapster flatten across the wire-flat boundary) has no dedicated skill — same gap logged in Pass-3b Step 4. Candidate: a `group-scalars-into-value-object` refactoring skill covering the read-site sweep + flatten map. Logged to lessons.
2. **Atomic build window.** Steps 1–3 leave the solution non-compiling until Step 4 — expected for a wide regroup, not a developer error. Done-gate is build-green after Step 4.
