# Pass 3a — DomainEnrichment: No-DTO-in-Domain (ADR-006)

**Feature Spec:** None (adhoc refactoring pass — scope in `docs/backlog.md` row 3 + `docs/architecture/decisions.md` **ADR-006**; user decisions in `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/questions.md` Q1/Q4/Q5, Answered. This is **Pass 3a ONLY** — the no-DTO-in-domain arm. Pass 3b (AppSettings VOs + Identity encapsulation) and Pass 3c (analytics computation → domain) are separate future runs and are explicitly NOT planned here.)

## Context

ADR-006: **no external/integration DTO enters the Domain.** Today `Fokus.Domain` project-references `Jira.Contracts` and 8 static factory methods on domain aggregates/entities perform the Jira/Xray → domain mapping inside the domain layer. ADR-006 requires dropping that reference and relocating the mapping to the **API boundary** (the sync feature folders), so the domain depends on nothing but `Blocks.Domain`.

Two services are impacted, both in `Fokus`:
- **`Fokus.Domain`** — loses the `Jira.Contracts` reference and the 8 `From*` factories.
- **`Fokus.API`** — gains boundary mapper classes in `Features/Sync/` and `Features/Xray/`; the two sync services (`SprintIssueSyncService`, `XrayIssueSyncService`) call the mappers instead of the domain factories. `Fokus.API` keeps its own `Jira.Contracts` reference (it already has one — 8 feature files use `JiraSprint`/`JiraIssue`).

The 8 factories split into **two groups** (this distinction drives the per-step mechanism — see Domain Model Changes):
- **Group 1 — true Jira-DTO mappers (5):** take a `Jira.Contracts` DTO (`JiraIssue`/`JiraSprint`) as input. These are the actual ADR-006 violations.
- **Group 2 — primitive constructors mis-named `From*` (3):** take only primitives/enums — no Jira/Xray DTO. They do not leak any DTO into the domain; the Xray DTO parsing already happens in `XrayIssueSyncService` before these are called. The user (Q4) directed relocating all 8 for consistency, so they move too — but as plain construction, not DTO mapping.

## Scope

**In scope:**
1. Create a Jira→domain boundary mapper in `Fokus.API/Features/Sync/` for the 5 Group-1 mappers, using Mapster `IRegister` with explicit member rules (Q4).
2. Create a domain-construction boundary mapper in `Fokus.API/Features/Xray/` for the 3 Group-2 factories (plain mapping; no Jira/Xray DTO involved).
3. Repoint the two sync services (`SprintIssueSyncService`, `XrayIssueSyncService`) to call the boundary mappers; delete the 8 domain `From*` factories.
4. Drop the `Fokus.Domain → Jira.Contracts` project reference and the `global using Jira.Contracts;` in `Fokus.Domain/GlobalUsings.cs`.
5. Remove the now-dangling `global using Jira.Contracts;` from `Fokus.Persistence/GlobalUsings.cs` (Persistence has no actual Jira usage — verified — and inherits the namespace only transitively through Domain; once Domain drops it, this line stops compiling).
6. Update `Fokus/CLAUDE.md` "Sync-from-external" line (it claims a `JiraMapper` exists — it never did; 3a creates the real boundary mapper).
7. Verification sweep + build.

**Explicitly out of scope (future runs — do NOT pre-empt):**
- **Pass 3b:** AppSettings flat-scalar → VO grouping (`BugRatioConfig`, `CycleTimeConfig`, `XrayConfig`, …), Identity `Person`/`User` encapsulation. Fokus sync aggregates **stay anemic** (public setters) this pass — the upsert sync path (B3, sanctioned) depends on them.
- **Pass 3c:** ADR-005 migration of the 15 analytics services to `Fokus.Domain/Analytics/` with endpoint-owned response DTOs.
- ADR-007 aggregate encapsulation (internal setters / ctor / Create factory). **Do not add any.** The `From*` factories are being *removed*, not converted into encapsulation factories.
- The `Fokus.Domain.Auth` (Invitation) coupling — Pass 4 (IdentityExtraction).
- Any behavior change to the sync logic, error handling, upsert semantics, or wire contracts. This is a pure relocation: same data, same field assignments, new home.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | `domain-service` (Input Boundary / ADR-006 section) | Follow | The ADR-006 rule the mappers enforce (DTOs map to domain at the boundary, before any domain code sees them); the 5 Group-1 mappers and their source/target pairs | — |
| 2 | (none) | — | 3 Group-2 primitive→domain constructions; plain mapper, NOT Mapster (no DTO) | Log to lessons (no skill for relocating a non-DTO construction helper out of the domain) |
| 3 | (none) | — | Repoint 2 sync services to the mappers; delete 8 domain factories; enumerated call sites | Log to lessons (cross-file relocation, no skill) |
| 4 | (none) | — | Drop `Fokus.Domain → Jira.Contracts` project ref + 2 stale global usings | Log to lessons (project-reference/global-using cleanup, no skill) |
| 5 | (none) | — | `Fokus/CLAUDE.md` doc correction | — |
| 6 | (none) | — | Verification sweep + build | — |

**Disposition notes:**
- **Step 1 Follow `domain-service`** — the skill's "Input Boundary — No DTOs Enter the Domain (ADR-006)" section is the governing pattern: external DTOs are mapped to domain types *at the boundary* before the domain sees them. The plan supplies only the 5 source→target pairs and the non-trivial member rules; the developer invokes the skill for the boundary-mapping principle and the Mapster `IRegister` convention (established in Pass 2). Do not restate the principle.
- **Steps 2–6 are None** — no skill covers relocating a non-DTO construction helper, repointing callers, dropping a project reference, doc edits, or the verification sweep. Logged as gaps; the strongest future-skill candidate is "relocate-domain-dto-factory" (same family as the Pass 2 Mapster-application gap).
- **No "Build."** All BuildingBlocks infrastructure (Mapster in CPM, `GlobalSettings.Scan` per-module DI) already exists from Pass 2. No infrastructure to build.

## Domain Model Changes

**Removals only — no additions.** Delete these 8 static factory methods (the *only* change to the domain this pass; the aggregates/entities themselves are untouched — same properties, same public setters):

**Group 1 — true Jira-DTO mappers (move to Step 1 Mapster `IRegister`):**
| Factory | File | Source DTO → Target | Non-trivial members (need explicit Mapster rules) |
|---------|------|---------------------|---------------------------------------------------|
| `Sprint.FromJira(JiraSprint, string boardName)` | `Fokus.Domain/Sprint/Behaviors/Sprint.cs` | `JiraSprint` → `Sprint` | `State` (string→`SprintState` enum, lowercase switch, default Active); `StartDate`/`EndDate` (null-coalesce to UtcNow / +14d); `BoardId` from `OriginBoardId`; `BoardName` from the extra `boardName` arg (not on the DTO); `SyncedAt = UtcNow` |
| `Ticket.FromJira(JiraIssue)` | `Fokus.Domain/Ticket/Behaviors/Ticket.cs` | `JiraIssue` → `Ticket` | `Id` from `Key`; nested `Fields.*` flattening; `EpicKey`/`EpicName`/`ParentTicketKey` (Epic-vs-parent conditional logic); several `?? "default"` coalesces |
| `Developer.FromJira(JiraIssue)` → `Developer?` | `Fokus.Domain/Developer/Behaviors/Developer.cs` | `JiraIssue` → `Developer?` | returns null when `Assignee` is null; `AvatarUrl` from `AvatarUrls["48x48"]` dictionary lookup; `IsActive = true` |
| `StatusTransition.ListFromJira(JiraIssue)` → `List<StatusTransition>` | `Fokus.Domain/Ticket/Behaviors/StatusTransition.cs` | `JiraIssue` → `List<StatusTransition>` | iterates `Changelog.Histories` ordered by `Created`, filters `Items` where `Field=="status"`; one-to-many — **not a 1:1 Mapster shape** (see note below) |
| `SprintMembership.FromJira(JiraIssue, Sprint, bool, int)` | `Fokus.Domain/Sprint/Behaviors/SprintMembership.cs` | `(JiraIssue, Sprint, forcedNotCommitted, planningWindowDays)` → `SprintMembership` | added/removed-at derivation from sprint changelog; `WasCommitted` planning-window business rule; multi-arg — **not a 1:1 Mapster shape** (see note below) |

**Group 2 — primitive constructors (move to Step 2 plain mapper; NO Mapster, NO DTO):**
| Factory | File | Inputs |
|---------|------|--------|
| `TestRun.FromXray(string id, string testExecutionIssueId, TestRunStatus, string statusName, DateTime?, DateTime?, string?)` | `Fokus.Domain/TestRun/Behaviors/TestRun.cs` | all primitives/enum — pure construction |
| `TestExecution.FromXray(string issueId, string issueKey, string summary, string status, string? assigneeId, DateTime createdDate)` | `Fokus.Domain/TestExecution/Behaviors/TestExecution.cs` | all primitives — pure construction |
| `TestSet.FromJiraIssueLink(string issueId, string issueKey, string summary, string? assigneeId, string status)` | `Fokus.Domain/TestSet/Behaviors/TestSet.cs` | all primitives — pure construction |

**Note on the two non-1:1 Group-1 maps (`StatusTransition.ListFromJira`, `SprintMembership.FromJira`):** these are one-to-many / multi-source with real business logic (changelog iteration, planning-window rule). They are NOT clean single-source Mapster `.Adapt<T>()` cases. Per ADR-010, non-trivial mapping that can't be Mapster is fine as an explicit mapper method — but it must live in the **boundary mapper class** (Step 1), not back in the domain. The developer's call on whether to express these as an `IRegister` `MapWith(...)` custom converter or a plain method *on the same boundary mapper class* — the binding rule is only that the logic leaves the domain and the domain stops referencing `Jira.Contracts`.

**`GetEffectiveSp(int defaultSpPerBug)` on `SprintMembership` STAYS** — it is genuine single-aggregate domain behavior (no DTO), not a factory. Do not touch it.

## Data Model Changes

None. No EF migration, no schema change, no entity-shape change. The boundary mappers produce the same domain objects with the same field values; persistence (upsert) is unchanged.

## Implementation Steps

**Ordering (binding):** Steps 1 + 2 (create mappers) before Step 3 (repoint callers + delete factories) before Step 4 (drop the reference — only safe once nothing in the domain uses `Jira.Contracts`). Step 5 (docs) any time. Step 6 verifies last. The build will be RED between Step 3 (factories deleted) and Step 4 (reference dropped) is the wrong order — delete factories and repoint callers (Step 3) *then* drop the reference (Step 4); do not drop the reference before the factories are gone.

### Step 1 — Jira→domain boundary mapper (5 Group-1 mappers) — ADR-006

**Disposition: Follow `domain-service`** (Input Boundary / ADR-006 section — boundary mapping principle + the Pass-2 Mapster `IRegister` convention). **Confidence: medium** — the 1:1 maps (`Sprint`, `Ticket`, `Developer`) are mechanical; the two non-1:1 maps (`StatusTransition`, `SprintMembership`) carry business logic that must move verbatim.

Create a boundary mapper in `src/Services/Fokus/Fokus.API/Features/Sync/` that maps the Jira DTOs to domain types, replacing the 5 Group-1 domain factories. Mapster `IRegister` with explicit member rules for the non-trivial members (Q4). The five source→target pairs and their non-trivial members are enumerated in **Domain Model Changes → Group 1**. Move each factory body's logic verbatim — same field assignments, same enum/default/derived rules, same null-return for `Developer`, same planning-window rule for `SprintMembership` — only the *location* changes.

- **Create** `src/Services/Fokus/Fokus.API/Features/Sync/JiraDomainMapper.cs` (or an `IRegister` named per the developer's call — the binding name is the *behavior*, not the class name; place it in `Features/Sync/`).
- The two non-1:1 maps (`StatusTransition.ListFromJira`, `SprintMembership.FromJira`) may be `MapWith` custom converters or plain methods on the same class — see the "Note on the two non-1:1 maps" above.
- Mapster is already in CPM and registered per-module via `GlobalSettings.Scan` (Pass 2) — no DI change needed; an `IRegister` in `Fokus.API` is auto-discovered. Reference an existing Pass-2 `IRegister` in `Features/Settings/` for the registration shape.

**Accept:**
- A boundary mapper exists under `src/Services/Fokus/Fokus.API/Features/Sync/` that produces `Sprint`, `Ticket`, `Developer?`, `List<StatusTransition>`, and `SprintMembership` from the Jira inputs.
- The `Developer` map returns null when `Assignee` is null; the `SprintMembership` map preserves the `WasCommitted` planning-window rule and added/removed-at derivation; the `Sprint` map preserves the lowercase-state switch and date null-coalescing. (Confirmed by reading against the original factory bodies — behavioral parity.)
- `dotnet build SprintRituals.slnx` green (the mapper compiles against `Jira.Contracts`, which `Fokus.API` already references).

### Step 2 — Xray/TestSet domain-construction boundary mapper (3 Group-2 factories)

**Disposition: None** (no DTO involved — not a `domain-service` ADR-006 case; no skill covers relocating a primitive construction helper). **Confidence: high** — pure field copy, no logic.

Move the 3 Group-2 factory bodies (`TestRun.FromXray`, `TestExecution.FromXray`, `TestSet.FromJiraIssueLink`) into a boundary mapper in `src/Services/Fokus/Fokus.API/Features/Xray/`. These take only primitives/enums — **do NOT force a Mapster `IRegister`** (there is no source DTO to map from; a Mapster map needs a source type). Plain static mapper methods (same signatures, same field assignments) are correct. The Xray DTO (`XrayTestExecutionDto`, etc.) parsing stays in `XrayIssueSyncService` exactly as today — only the final domain-object construction moves.

- **Create** `src/Services/Fokus/Fokus.API/Features/Xray/XrayDomainMapper.cs` (name is the developer's call; place it in `Features/Xray/`) with three methods producing `TestRun`, `TestExecution`, `TestSet` from the same primitive inputs the factories took.
- Inputs enumerated in **Domain Model Changes → Group 2**.

**Accept:**
- Three mapper methods under `src/Services/Fokus/Fokus.API/Features/Xray/` produce `TestRun`/`TestExecution`/`TestSet` with field assignments identical to the deleted factories.
- No Mapster `IRegister` was added for these three (verify: the new Xray mapper contains no `: IRegister`) — they are plain construction.
- `dotnet build SprintRituals.slnx` green.

### Step 3 — Repoint the two sync services + delete the 8 domain factories

**Disposition: None** (cross-file relocation; no skill). **Confidence: high** — call sites are fully enumerated below.

Replace every call to a domain `From*` factory with a call to the new boundary mapper (Step 1/2), then delete the 8 factory methods from the domain. Grep-verify zero remaining callers before deleting.

**Call sites to repoint (verified — all 13 calls):**
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs`: `Sprint.FromJira` (:58), `Ticket.FromJira` (:120, :154), `Developer.FromJira` (:123, :157), `StatusTransition.ListFromJira` (:164), `SprintMembership.FromJira` (:167).
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs`: `TestExecution.FromXray` (:229), `TestRun.FromXray` (:259), `TestSet.FromJiraIssueLink` (:290).

**Factories to delete (the partial-class behavior files become empty of the factory — delete the method; if the file has no other member, the developer may delete the file, but `SprintMembership` behavior file KEEPS `GetEffectiveSp` so only the factory goes there):**
- `Sprint/Behaviors/Sprint.cs` — delete `FromJira` (keep `AddMembership`).
- `Ticket/Behaviors/Ticket.cs` — delete `FromJira` (file becomes empty → may delete file).
- `Developer/Behaviors/Developer.cs` — delete `FromJira` (file becomes empty → may delete file).
- `Ticket/Behaviors/StatusTransition.cs` — delete `ListFromJira` (file becomes empty → may delete file).
- `Sprint/Behaviors/SprintMembership.cs` — delete `FromJira` only; **KEEP `GetEffectiveSp`**.
- `TestRun/Behaviors/TestRun.cs`, `TestExecution/Behaviors/TestExecution.cs`, `TestSet/Behaviors/TestSet.cs` — delete the single factory each (files become empty → may delete file).

**Verification sub-step (binding — grep for all callers before deleting):** `rg -n "\.FromJira|\.FromXray|FromJiraIssueLink|ListFromJira" src --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'` must show only the new boundary-mapper internals (or zero) after repointing — no call from any sync service or other feature. If any unexpected caller appears, STOP and flag (questions.md to architect).

**Accept:**
- The two sync services call the boundary mappers; no `Sprint/Ticket/Developer/StatusTransition/SprintMembership/TestRun/TestExecution/TestSet . From*` call remains in `Fokus.API` outside the mapper classes.
- All 8 factory methods are gone from `Fokus.Domain`; `GetEffectiveSp` remains.
- `dotnet build SprintRituals.slnx` green.

### Step 4 — Drop the `Fokus.Domain → Jira.Contracts` reference + stale global usings

**Disposition: None** (project-reference + global-using cleanup; no skill). **Confidence: high.**

Only after Step 3 (domain no longer uses any `Jira.Contracts` type), remove the coupling so the domain depends on nothing but `Blocks.Domain`.

- **Modify** `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — remove `<ProjectReference Include="..\..\..\Modules\Jira\Jira.Contracts\Jira.Contracts.csproj" />` (line 11).
- **Modify** `src/Services/Fokus/Fokus.Domain/GlobalUsings.cs` — remove `global using Jira.Contracts;` (line 4).
- **Modify** `src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` — remove `global using Jira.Contracts;` (line 3). **Reason:** Persistence has zero actual Jira usage (verified) and inherits the `Jira.Contracts` namespace only transitively through the Domain reference; once Domain drops the reference this global using stops compiling. This is the transitive break that would otherwise surface as a confusing mid-build error.

**Accept:**
- `rg -n "Jira" src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` → zero hits.
- `rg -n "Jira\.|using Jira" src/Services/Fokus/Fokus.Domain --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'` → zero hits (domain is Jira-free).
- `rg -n "Jira" src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` → zero hits.
- `dotnet build SprintRituals.slnx` green (proves Persistence had no real Jira dependency).

### Step 5 — Correct the stale `Fokus/CLAUDE.md` sync-mapping line

**Disposition: None** (doc correction). **Confidence: high.**

`src/Services/Fokus/CLAUDE.md` "Key patterns → Sync-from-external" says *"`JiraMapper` maps Jira DTOs → domain entities."* No `JiraMapper` ever existed (the `From*` factories did the mapping). 3a creates the real boundary mapper. Update the line to describe the actual ADR-006 boundary mapping: Jira/Xray DTOs are mapped to domain types by a boundary mapper in `Features/Sync/` + `Features/Xray/` (the domain references no external contracts). Do not invent other doc changes.

**Accept:**
- `Fokus/CLAUDE.md` no longer claims a `JiraMapper`; it names the `Features/Sync` + `Features/Xray` boundary mapping and the no-DTO-in-domain rule (ADR-006).

### Step 6 — Verification sweep + build

**Disposition: None** (verification). **Confidence: high.** This is the objective done-gate.

1. `rg -n "\.FromJira|\.FromXray|FromJiraIssueLink|ListFromJira" src --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'` → only boundary-mapper internals or zero; no domain factory definitions remain.
2. `rg -n "Jira\.|using Jira|JiraIssue|JiraSprint" src/Services/Fokus/Fokus.Domain --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'` → zero (domain Jira-free).
3. `rg -n "Jira" src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` → zero.
4. Boundary mappers exist: one under `Features/Sync/` (Group 1, Mapster `IRegister`), one under `Features/Xray/` (Group 2, plain, no `IRegister`).
5. `GetEffectiveSp` still present on `SprintMembership`.
6. `dotnet build SprintRituals.slnx` → green (filter `error CS`; ignore MSB3026/3027/3492 file-lock per `project-rules`).

**Accept:** all 6 checks pass; build green; implementation.md records each grep's actual output.

## Cross-Service Changes

None across module boundaries. `Jira.Contracts` (the `Jira` module) is unchanged. gRPC, integration events (`SprintSynced` still published unchanged in `SprintIssueSyncService`), and the Vue client are untouched — this is an internal layering change with no wire impact.

## Migration Notes

No EF migration (no schema change). No seed-data change. The package set is unchanged (Mapster already in CPM from Pass 2). `dotnet build` is the only restore needed.

## Testing Strategy

No automated suite (project decision — manual testing after each pass per `docs/backlog.md`). Verification is:
1. **Grep-checkable acceptance** per step (Step 6 battery) — the primary objective gate.
2. **Build** — `dotnet build SprintRituals.slnx` green after each code step (and the binding ordering: factories+callers in Step 3 before the reference drop in Step 4).
3. **Behavioral parity (read-level):** for each relocated map, confirm the field assignments match the original factory body verbatim (Step 1/2 accept) — especially the three with logic: `Sprint` state/date defaults, `Developer` null-return + avatar lookup, `SprintMembership` planning-window `WasCommitted`, `StatusTransition` changelog iteration.
4. **Manual smoke (user, post-pass):** run a Jira sprint sync (`POST /api/sync/sprints`) and confirm sprints, tickets, developers, status transitions, and memberships persist identically; with Xray enabled, confirm test executions / runs / sets still sync. No output should differ from pre-3a — this pass changes structure, not behavior.

## KB Impact

None. `docs/kb/` does not exist (verified — no `docs/kb/index.md`). 3a relocates mapping code; it changes no business rule, formula, or edge case. No KB step warranted.

## Open Questions

None blocking — Q1/Q4/Q5 are Answered (questions.md). Two developer-time judgments are flagged inline, not blockers:
- **Step 1 — the two non-1:1 maps** (`StatusTransition.ListFromJira`, `SprintMembership.FromJira`): express as Mapster `MapWith` custom converters or plain methods on the boundary mapper class — developer's call. Binding rule: the logic leaves the domain.
- **Step 3 — empty behavior-file deletion:** files that hold only a deleted factory may be removed; `Sprint/Behaviors/Sprint.cs` and `Sprint/Behaviors/SprintMembership.cs` must NOT be deleted (they keep `AddMembership` / `GetEffectiveSp`).

**Flags for the team-lead (not blockers):**
1. **Boundary-mapper skill gap** — Steps 1–2 have no dedicated skill for relocating a domain DTO-factory to the boundary. Strongest candidate for a new `relocate-domain-dto-factory` skill (same family as the Pass-2 Mapster-application gap). Logged to lessons.
2. **Group-2 naming** — the 3 primitive factories are named `From{Xray,JiraIssueLink}` but take no DTO. After relocation their boundary-mapper method names should drop the misleading `From*`-DTO framing (developer's call); flagged so the reviewer doesn't read the rename as scope creep.
