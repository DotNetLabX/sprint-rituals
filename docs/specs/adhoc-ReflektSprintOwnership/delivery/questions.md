# adhoc-ReflektSprintOwnership (RA3) — Questions

These are the judgment calls in the RA3 scope (`Rename Sprint→RetroSession; add 1..N Fokus-sprint reference; FokusSprintSnapshot projection; consume SprintSynced; drop standalone sprint creation — ADR-017/018/019`) that the ADRs do not fully settle. Each carries a recommendation + confidence. Questions are routed `To: user` because every one is a scope/risk-tolerance decision, not an architect-adjudicable code fact. Grounded against committed `main` (HEAD `ca1ecd6`), source read directly.

---

## Q1: How far does the `Sprint`→`RetroSession` rename extend — does it include the `SprintState`/`SprintPhase`/`SprintAction`/`ISprintAction` retro-state types and the child-entity `SprintId` FK columns?

**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** The token `Sprint` appears 153 times across 59 files in `src/Services/Reflekt`. They split into three distinct meanings:
1. **The aggregate** = the retro session itself (`Sprint.cs`, `SprintRepository`, `SprintConfiguration`, `Sprints` DbSet, table `"Sprints"`, the `Features/Sprints/*` slice, `SprintContracts`, `SprintMappings`, `SprintCreatedEvent`/`SprintStartedEvent`/`SprintClosedEvent`, `SprintHub`). ADR-018 explicitly renames this to `RetroSession`.
2. **Retro-state homonyms** — `SprintState` (Draft/Active/Closed), `SprintPhase` (Writing/Grouping/Voting/Discussing/Reporting), `SprintAction`, `ISprintAction`. These name the *retro session's* lifecycle, not a Jira sprint. They are the "non-call homonym" carve-out from the type-move rules.
3. **Child-entity FK columns** — `Participant.SprintId`, `Card.SprintId`, `CardGroup.SprintId`, `Vote` (via group), the `{SprintId, PersonId}` unique index, the `"sprintId"` SignalR claim, the `"sprint:{id}"` SignalR group prefix, the `JoinSprint`/`LeaveSprint` hub methods.

ADR-018 names only "rename from the current `Sprint` aggregate." It does not say whether the rename cascades into the homonym enums or the FK column names. There is a real cost/clarity tradeoff and a wire-stability dimension (see Q2).

**Question:** Which of these three layers does RA3 rename?
- (a) Aggregate only — type `Sprint`→`RetroSession`, `Sprints` DbSet→`RetroSessions`, repo/config/contracts/events/hub class. Leave `SprintState`/`SprintPhase`/etc. and the `SprintId` FK columns as-is.
- (b) Aggregate + state/phase types renamed to `RetroSessionState`/`RetroPhase` etc., but FK columns stay `SprintId`.
- (c) Full sweep — aggregate + state types + every `SprintId` FK column → `RetroSessionId`, the SignalR group/claim/method names too.

**Recommendation:** **(b)** — rename the aggregate AND the retro-state types (`SprintState`→`RetroSessionState`, `SprintPhase`→`RetroPhase`, `SprintAction`/`ISprintAction`→`RetroAction`/`IRetroAction`), because after RA3 the word "Sprint" inside Reflekt becomes genuinely ambiguous (a `RetroSession` will now reference Fokus *sprints* — leaving a `SprintPhase` enum on it actively misleads). **Keep the child FK columns named `SprintId`** in this pass: renaming them to `RetroSessionId` is a large, purely-cosmetic SQLite table-rebuild across 4 child tables + indexes + the SignalR claim/group/method wire surface, with zero behavioral payoff, and it muddies the bisect between "ownership semantics" and "column cosmetics." Option (c)'s column/wire rename is better deferred to RA5 (frontend merge), where the wire contract is being touched anyway. I will state the `SprintId`-stays carve-out as an explicit DO-NOT-TOUCH list with the rename table.
**Confidence:** medium — (b) is the clean-semantics reading and matches ADR-018's intent, but the homonym-vs-cosmetic line is a genuine taste call; if you'd rather keep RA3 minimal (aggregate only, defer all enum churn) that is defensible too, and (a) is lower-risk.

### Answer
**(b) — user-confirmed (2026-06-15).** Rename aggregate + retro-state types (`SprintState`→`RetroSessionState`, `SprintPhase`→`RetroPhase`, `SprintAction`/`ISprintAction`→`RetroAction`/`IRetroAction`). Keep child FK columns named `SprintId` (explicit DO-NOT-TOUCH list). FK-column rename deferred to RA5.

---

## Q2: Wire stability — must RA3 preserve the Reflekt HTTP routes (`/api/teams/{teamId}/sprints`), the `SprintResponse`/`SprintSummaryResponse` JSON field names, the `/hubs/retro` hub method names (`JoinSprint`/`LeaveSprint`), the `"sprint:{id}"` group, and the `"sprintId"`/`participantId` claims — or may RA3 rename them freely?

**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** A frontend sweep of `client` found **no Reflekt/retro frontend in this repo at all** — `client/` is the Fokus dashboard SPA only (its `/sprints` routes are Fokus analytics, unrelated). The Reflekt Vue app still lives in the un-merged original Reflekt project repo and is scheduled for **RA5 (`adhoc-ReflektFrontendMerge`, still Blocked)**. So **no consumer in this repo reads Reflekt's sprint/retro wire surface today.** Wire stability is therefore a *future-RA5* concern, not a current-break concern. This is the same situation RA2 faced with `JoinSprintResponse.UserId` (kept stable via `[JsonPropertyName]` as a precaution).

**Question:** For Reflekt's wire surface specifically (routes, JSON field names, SignalR method/group/claim strings):
- (a) **Rename freely now** — `/api/teams/{teamId}/retro-sessions`, `RetroSessionResponse`, `JoinRetro`/`LeaveRetro`, `retro:{id}`, `retroSessionId` claim. RA5 adapts the merged frontend to whatever RA3 lands.
- (b) **Preserve the wire strings** — rename only backing C# types; keep route literals, JSON property names, hub method/group/claim strings byte-stable so the eventual RA5 merge of the existing client from the original Reflekt project needs no wire changes.

**Recommendation:** **(a) for HTTP routes + JSON shapes, (b) for SignalR method/group/claim strings.** Rationale: the HTTP route/response rename (`sprints`→`retro-sessions`) is the *visible* half of the ADR-018 ownership change and RA5 must adapt the merged client to the renamed domain regardless — keeping `sprints` routes on a `RetroSession` aggregate would re-introduce the exact ambiguity ADR-018 removes. But the SignalR layer (`JoinSprint`, `"sprint:{id}"`, `"sprintId"`/`participantId`/`displayName` claims) is internal real-time plumbing with no analytics value, churning it now means re-coordinating the guest-token claim minting + the hub in the same pass for no semantic gain — defer to RA5 with the rest of the real-time wiring. Net: this pairs with Q1(b) cleanly — rename HTTP/domain, hold the SignalR + FK-column layer for RA5. I will mark the SignalR strings as a binding DO-NOT-TOUCH carve-out.
**Confidence:** medium — depends on how you want to sequence RA5. If you'd rather RA3 land the *complete* Reflekt-side rename in one shot (so RA5 is pure frontend with no backend wire surprises), choose (a) for everything; that is a coherent alternative. The split I recommend optimizes bisectability over one-shot completeness.

### Answer
**Split — user-confirmed (2026-06-15).** Rename HTTP routes (`/sprints`→`/retro-sessions`) + JSON shapes freely. PRESERVE SignalR method/group/claim strings (`JoinSprint`/`LeaveSprint`, `"sprint:{id}"`, `"sprintId"`/`participantId`/`displayName` claims) as a binding DO-NOT-TOUCH carve-out for RA5.

---

## Q3: "Drop standalone sprint creation" — delete the `CreateSprint` endpoint, or require ≥1 Fokus-sprint reference at retro creation?

**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** The RA3 backlog row says "drop standalone sprint creation (ADR-017/018/019)." Today `CreateSprintEndpoint` (`POST /api/teams/{teamId}/sprints`) lets an SM create a retro with just a name + column config — no Fokus link. ADR-017 says Reflekt does not create or own *Jira* sprints; ADR-018 says a `RetroSession` references 1..N Fokus sprints. But a `RetroSession` must still be *created* somehow (it's the retro board). "Drop standalone creation" most plausibly means "you can no longer create a retro that isn't tied to ≥1 Fokus sprint," **not** "delete the create endpoint." Two readings → genuinely different work:
- (a) **Delete `CreateSprintEndpoint` entirely.** Retros are created by some other trigger (e.g. on `SprintSynced`, or by a future RA4 flow). But nothing in RA3's named scope provides a replacement creation path, which would leave retros uncreatable until a later pass — almost certainly not intended.
- (b) **Keep creation, require Fokus-sprint selection.** `CreateRetroSessionCommand` gains a required `int[] FokusSprintIds` (1..N, ADR-018); the handler validates ≥1, persists the link rows, and the `FokusSprintSnapshot` for each referenced sprint is populated (from the local projection that `SprintSynced` maintains). The column-config + timer-default creation behavior is otherwise preserved.

**Question:** Does "drop standalone sprint creation" mean (a) delete the endpoint, or (b) keep creation but make a Fokus-sprint reference mandatory?

**Recommendation:** **(b)** — require ≥1 Fokus-sprint reference at retro creation; rename the endpoint/command to `CreateRetroSession*`. This is the reading consistent with ADR-018 ("a RetroSession references 1..N Fokus sprints") and with the program still being able to create retros after RA3. "Standalone" = "not tied to a Fokus sprint" — that is the capability being dropped, not the creation feature. I will scope the link as a child collection on `RetroSession` (a `FokusSprintRef` value object / owned collection keyed by Fokus `SprintId`) plus the `FokusSprintSnapshot` read-projection per ADR-019, and have creation validate that each referenced `SprintId` has a known snapshot (or accept unknown ids and let `SprintSynced` backfill — that sub-decision I'll resolve in the plan, flagging it if it needs your call).
**Confidence:** medium — (b) is the only reading that keeps the system coherent after RA3, but I'm flagging it because the backlog wording literally says "drop … creation," and if you intend retros to be auto-created from Fokus sprints (no SM create form at all) that is a materially different design I should not assume.

### Answer
**(b) — user-confirmed (2026-06-15).** Keep creation; rename endpoint/command to `CreateRetroSession*`; require ≥1 `FokusSprintId` (1..N, ADR-018). "Standalone" = "untied to a Fokus sprint" is the dropped capability, not the creation feature. The known-vs-unknown-snapshot sub-decision is left to the architect to resolve in the plan (flag if it needs a user call); given Q4(a) forward-only, accept-unknown-ids + backfill is expected.

---

## Q4: How does a referenced Fokus sprint get its `FokusSprintSnapshot` — eager pull at retro-create, or lazy on the next `SprintSynced`? And what populates a snapshot for a sprint that was synced *before* Reflekt started consuming `SprintSynced`?

**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** ADR-019: `SprintSynced` (Fokus→Reflekt, already published, carries `SprintId/SprintName/BoardId/SyncedAt/State`) is consumed by Reflekt to refresh the `FokusSprintSnapshot` projection — "one row per referenced Fokus sprint, overwritten on re-sync." The gap: `SprintSynced` fires on *every* Fokus sync going forward, but a retro created in RA3 references a sprint that may have last synced *before* the Reflekt consumer existed — so no snapshot row exists yet. The richer metrics (`completion/disruption/carry-over/health`, per `AnonymizedSprintSummary`) are NOT in the `SprintSynced` payload — they come from the gRPC `ISprintAnalyticsService`, whose client is **RA4** (`adhoc-SprintContextPanel`), not RA3.

**Question:** In RA3, what does the `FokusSprintSnapshot` contain and how is it first populated for a referenced sprint?
- (a) **Event-only, forward-only.** Snapshot carries just what `SprintSynced` gives (`SprintId, SprintName, BoardId, SyncedAt, State`). A referenced sprint with no prior event has an empty/absent snapshot until its next Fokus sync. The rich metrics + on-demand gRPC pull are entirely RA4's job.
- (b) **Event + eager gRPC pull at create.** RA3 also wires the `ISprintAnalyticsService` client now and pulls metrics when a retro references a sprint, so the snapshot is populated immediately.

**Recommendation:** **(a) — event-only, forward-only snapshot in RA3.** ADR-019 splits the two channels deliberately: `SprintSynced`→projection (RA3) and gRPC `AnonymizedSprintSummary`→on-demand panel (RA4, per the backlog RA4 row which explicitly owns the `ISprintAnalyticsService` client). Pulling the gRPC client into RA3 would (i) duplicate RA4's named scope, (ii) couple RA3 to the Fokus analytics surface ahead of its pass, and (iii) break the lift-then-integrate bisect discipline. The snapshot in RA3 should hold exactly the `SprintSynced` fields; the "stale/missing snapshot" case is acceptable because the rich metrics + their freshness are RA4's concern. I'll document the forward-only limitation as a known carry-over to RA4 (a sprint referenced but never re-synced shows a thin/absent snapshot until RA4's gRPC pull lands).
**Confidence:** high — this is what ADR-019 + the RA3/RA4 backlog split directly specify; (b) would be scope-stealing from RA4. Flagging only so you can confirm the thin-snapshot-until-RA4 limitation is acceptable.

### Answer
**(a) — user-confirmed (2026-06-15).** Event-only, forward-only snapshot holding exactly the `SprintSynced` fields (`SprintId, SprintName, BoardId, SyncedAt, State`). No gRPC client in RA3 — rich metrics + on-demand pull are RA4's scope. Thin/absent-snapshot-until-RA4 limitation accepted; document as a carry-over to RA4.

---

## Q5: Is there an existing `reflekt.db` whose data must survive the rename migration, or is it disposable (as in RA2)?

**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** RA3 renames the `Sprints` table → `RetroSessions` (Q1) and adds the Fokus-sprint-link table + `FokusSprintSnapshot` table. On SQLite a table rename is a rebuild. RA2's ratified position (Q3c) was "no backfill, dev `reflekt.db` disposable" — its migration `Down()` was accepted as not data-reversible. If RA3 inherits that stance, the rename migration can be a straightforward rebuild with no data-migration ceremony.

**Question:** Same disposable-dev-DB stance as RA2 (rename migration need not preserve existing rows; `Down()` need not be data-reversible)?

**Recommendation:** **Yes — inherit RA2's disposable-dev-DB stance.** No production `reflekt.db` exists (Reflekt has never been deployed from this monolith — the lift is in-flight), and RA2 already set this precedent one pass ago for the same database. The migration is schema-correct and `Up()`-only meaningful; I'll note `Down()` as schema-reversible-but-not-data-reversible, consistent with RA2.
**Confidence:** high — directly inherits a ratified one-pass-old decision on the same DB; flagging only to confirm nothing changed (e.g. you didn't seed a `reflekt.db` you care about since RA2).

### Answer
**Yes — user-confirmed (2026-06-15).** Inherit RA2's disposable-dev-DB stance: rename migration need not preserve rows; `Down()` is schema-reversible-but-not-data-reversible.
