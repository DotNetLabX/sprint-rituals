# adhoc-ReflektFrontendMerge (RA5-C) — Questions

## Q1: DTO count discrepancy — plan says 23 remaining, code has 26

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 2 (Flat route family: request DTOs)
**File:** `src/Services/Reflekt/Reflekt.API/Features/**/*Endpoint.cs`, `**/*Command.cs`

**Context:** The plan states "23 request/command DTOs (3 are already migrated — `SetReviewStatusCommand`, `GetSprintReportRequest`, `SprintContextRequest`; finish the rest)." Code grep shows 26 request/command records with a `SprintId` property on the `/api/sprints/{sprintId}/...` flat routes. The 3 already-migrated ones are confirmed (`SetReviewStatusCommand`, `GetSprintReportRequest`, `SprintContextRequest`). That leaves 26 un-migrated, not 23.

The 3 that explain the discrepancy (present in code, not in the plan's implied tally):
- `UpdateColumnsCommand(int SprintId, ...)` — route `/api/sprints/{sprintId}/columns`
- `GetSprintSummaryRequest(int SprintId)` — route `/api/sprints/{sprintId}/summary`
- `JoinSprintCommand(int SprintId, ...)` — route `/api/sprints/{sprintId}/join`

All three are board-meaning identifiers on flat `/api/sprints/` routes and are NOT carve-outs.

**Question:** The count in the plan (23) is a minor undercount — the actual target is 26. Should implementation proceed with all 26 (the correct set), treating this as a plan typo, or does the architect want to re-examine whether any of the 3 extra should be scoped differently?

**Recommendation:** Proceed with all 26. The acceptance grep in Step 2 is the real guard (`rg "/api/sprints/" src/Services/Reflekt client/apps/retro` → zero); the "23" was a count error. All 26 are clearly board-meaning and not carve-outs.
**Confidence:** high — all 26 are unambiguous board-meaning identifiers; the acceptance grep catches them all; the 3-record discrepancy is a count error, not a boundary question.

### Answer
**RULING: Proceed with all 26. APPROVED.** (architect, 2026-06-20 — confidence: high)

Verified against source:
- `[HttpX("/api/sprints/{sprintId}/…")]` flat-route declarations = **exactly 26** (`rg '\[Http[A-Za-z]+\("/api/sprints/' src/Services/Reflekt --type cs | wc -l` → 26).
- The 3 records the plan's tally missed — `UpdateColumnsCommand`, `GetSprintSummaryRequest`, `JoinSprintCommand` — are confirmed present, all carry `int SprintId`, all on flat `/api/sprints/` routes, all unambiguously board-meaning. None is a carve-out.
- The plan's "23" is a count error, not a scope statement. The binding endgame is "eliminate **every** board-meaning `sprint` identifier" — a complete rename has no per-DTO budget. The acceptance grep `rg "/api/sprints/" src/Services/Reflekt client/apps/retro → zero` is the real gate and it enforces all 26 regardless of the count in the prose.

**Plan correction (architect will apply):** Step 2's "**23 request/command DTOs**" → "**26**". This is a typo fix, not a scope change — no re-approval needed. Rename `SprintId → RetroSessionId` on all 26; carve-out unaffected.

---

## Q2: VoteRepository.GetParticipantIdsWithVotesInSprintAsync not in plan's Step 1 rename list

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 1 (Board FK rename: repo method family)
**File:** `src/Services/Reflekt/Reflekt.Persistence/Repositories/VoteRepository.cs:42`

**Context:** The plan's Step 1 explicitly lists the repo method families to rename:
- `CardRepository.GetBySprintIdAsync`, `GetUnreviewedActionItemsBySprintIdAsync`, `CountActionItemsBySprintIdAsync`, `GetActionItemsReviewedInSprintAsync`
- `ParticipantRepository.GetBySprintIdAsync`
- `CardGroupRepository.GetBySprintIdAsync`

`VoteRepository` also has `GetParticipantIdsWithVotesInSprintAsync(int sprintId, ...)` (line 42), which contains `InSprint` (board meaning) and takes a `sprintId` param that means the retro board. The Step 1 acceptance grep (`rg "SprintId" src/Services/Reflekt --type cs -g '!**/Migrations/**' -g '!**/FokusSprint*'` piped through `rg -v "FokusSprintId"`) would catch the `g.SprintId == sprintId` inside this method (line 44) — so the acceptance criterion requires renaming it, even if the plan's explicit list omits it.

**Question:** `VoteRepository.GetParticipantIdsWithVotesInSprintAsync` must be renamed to make the Step 1 acceptance grep pass. Should it become `GetParticipantIdsWithVotesInRetroSessionAsync` or `GetParticipantIdsWithVotesForRetroSessionAsync`? (Pattern from the other repos: `…ByRetroSessionId…`; this method doesn't follow the `ByRetroSessionId` suffix pattern since it navigates via CardGroup.)

**Recommendation:** Rename to `GetParticipantIdsWithVotesForRetroSessionAsync` (avoids the `ById` inaccuracy since the method filters via `g.SprintId == sprintId` inside the query, not a direct PK/FK lookup). Update its call site in `AdvancePhaseCheckEndpoint.cs:86`.
**Confidence:** high — the acceptance grep enforces it; name choice is the only question.

### Answer
**RULING: Rename it. Use `GetParticipantIdsWithVotesForRetroSessionAsync`. APPROVED.** (architect, 2026-06-20 — confidence: high)

Verified against source:
- `VoteRepository.cs:42` — method `GetParticipantIdsWithVotesInSprintAsync(int sprintId, …)`, body filters `g.SprintId == sprintId` (line 44). The name carries `InSprint` (board meaning) and the param is the board id. Both are board-meaning identifiers → in scope.
- Sole call site is `AdvancePhaseCheckEndpoint.cs:86` (already passes `request.RetroSessionId` — the caller side is half-renamed, the callee name is not; this is exactly the partial-rename inconsistency the endgame forbids).
- Step 1's widened acceptance grep (`SprintId` minus `FokusSprintId`/carve-out) **will** flag `g.SprintId == sprintId` on line 44 → leaving the method un-renamed fails the gate. The plan's explicit list is illustrative, not exhaustive; the grep is binding. This is an instance of the plan's own "a `\b`-anchored grep misses these … yet they encode the renamed concept" rule.

**Name:** `GetParticipantIdsWithVotesForRetroSessionAsync` — your recommendation is correct. The `…ByRetroSessionId…` suffix the sibling repos use implies a direct FK/PK lookup; this method navigates via `CardGroup.SprintId`, so `…ForRetroSession…` is the accurate form. Internal method name → developer's call, but this is the right one. Update the call site at `AdvancePhaseCheckEndpoint.cs:86` in the same edit.

**No plan change needed** — Step 1 already mandates "rename the board-meaning repo method family … so the widened acceptance grep returns zero." This method is a member of that family the explicit list under-enumerated; the grep covers it.

---

## Q3: Frontend `sprint.ts` and `PreviousActionsPanel.vue` call `/api/teams/{teamId}/sprints/…` routes — backend already uses `/api/teams/{teamId}/retro-sessions/…`

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 2 (Flat route family — frontend callers)
**File:** `client/apps/retro/src/stores/sprint.ts`, `client/apps/retro/src/components/retro/PreviousActionsPanel.vue`

**Context:** The plan's Step 2 states the frontend scope as "~30 call sites" in the seven stores, listing the flat `/api/sprints/${sprintId}/…` → `/api/retro-sessions/${retroSessionId}/…` rename. However, several frontend files also call **nested** `/api/teams/${teamId}/sprints/${sprintId}/…` routes that the backend already serves under `/api/teams/${teamId}/retro-sessions/${retroSessionId}/…`:

`sprint.ts` (7 team-scoped calls — currently 404-ing against the backend):
- Line 85: `/api/teams/${teamId}/sprints` → backend is `/api/teams/${teamId}/retro-sessions`
- Line 90: `/api/teams/${teamId}/sprints/${sprintId}` → backend is `/api/teams/${teamId}/retro-sessions/${retroSessionId}`
- Line 102: `/api/teams/${teamId}/sprints` (create)
- Line 134: `/api/teams/${teamId}/sprints/${sprintId}/start`
- Line 145: `/api/teams/${teamId}/sprints/${sprintId}/advance-phase/check`
- Line 155: `/api/teams/${teamId}/sprints/${sprintId}/advance-phase`
- Line 166: `/api/teams/${teamId}/sprints/${sprintId}/close`

`report.ts` (1 team-scoped call):
- Line 85: `/api/teams/${teamId}/sprints/${sprintId}/report` → backend is `/api/teams/${teamId}/retro-sessions/${retroSessionId}/report`

`PreviousActionsPanel.vue` (1 inline call):
- Line 112: `/api/teams/${teamId}/sprints/${sprintId}/previous-actions/${cardId}` → backend is `/api/teams/${teamId}/retro-sessions/${retroSessionId}/previous-actions/${cardId}` (SetReviewStatusEndpoint)

These 9 team-scoped calls are currently broken (404s) since the backend switched to `retro-sessions` in RA3, and they are outside the plan's stated scope (plan only targets flat `/api/sprints/` calls). The Step 2 acceptance grep (`rg "/api/sprints/" src/Services/Reflekt client/apps/retro` → zero) does NOT catch these `/api/teams/{teamId}/sprints/` strings — they use a different prefix.

**Question:** Should the RA5-C Step 2 implementation also fix these 9 team-scoped frontend calls (extending scope), or are they deferred to a follow-up? The plan's acceptance grep won't catch them, and leaving them means 9 broken network calls in the merged retro app.

**Recommendation:** Fix all 9 in Step 2 — they are broken *today* and are the same rename operation (sprints → retro-sessions) on the frontend. The acceptance grep for team-scoped routes can be added as a parallel check: `rg "/api/teams/.*/sprints" client/apps/retro` → zero.
**Confidence:** medium — fixing is clearly correct semantically, but it extends the plan's stated scope. The architect may prefer a separate step or a follow-up ticket given the plan was approved with the current acceptance grep.

### Answer
**RULING: FOLD IN. Fix all 9 in Step 2, add the complementary acceptance grep. — architect recommendation, confidence: HIGH. This is a scope call; team lead takes it to the user for the final yes.** (architect, 2026-06-20)

**This is not a new feature — it is the missing half of the operation Step 2 already owns.** The locked endgame (parent plan, owner 2026-06-19) is the *complete* `Sprint→RetroSession` rename with **lockstep** frontend, "frontend is in-repo so all renames are lockstep with no external consumer." Step 2's stated job is precisely "rename the board routes + update the in-repo retro frontend callers in lockstep." The 9 nested `/api/teams/${teamId}/sprints/…` calls are board-route callers that Step 2's acceptance grep simply *failed to catch* because it only matches the flat `/api/sprints/` prefix. The scope was always "all board-route frontend callers"; the grep under-specified it. Folding in is closing a hole in the plan's own acceptance, not extending its intent.

Verified against source (all 9 backend targets exist — these are live 404s, not speculative):

| Frontend call (broken today) | Backend route confirmed present |
|---|---|
| `sprint.ts:85` `/api/teams/{teamId}/sprints` | `GetTeamSprintsEndpoint` → `…/retro-sessions` ✓ |
| `sprint.ts:90` `…/sprints/{id}` | `GetSprintEndpoint` → `…/retro-sessions/{retroSessionId}` ✓ |
| `sprint.ts:102` `…/sprints` (create) | `CreateSprintEndpoint` → `…/retro-sessions` ✓ |
| `sprint.ts:134` `…/sprints/{id}/start` | `StartSprintEndpoint` ✓ |
| `sprint.ts:145` `…/sprints/{id}/advance-phase/check` | `AdvancePhaseCheckEndpoint` ✓ |
| `sprint.ts:155` `…/sprints/{id}/advance-phase` | `AdvancePhaseEndpoint` ✓ |
| `sprint.ts:166` `…/sprints/{id}/close` | `CloseSprintEndpoint` ✓ |
| `report.ts:85` `…/sprints/{id}/report` | `GetSprintReportEndpoint` ✓ |
| `PreviousActionsPanel.vue:112` `…/sprints/{id}/previous-actions/{cardId}` | `SetReviewStatusEndpoint` (PATCH) ✓ |

**Why fold in rather than defer (the three reasons that make this the right call):**
1. **They are broken in production *now*.** The backend moved to `retro-sessions` in RA3; these 9 calls 404 against the live backend. Deferring leaves the merged retro app with a dead create/list/start/advance/close/report path — i.e. the core session lifecycle is non-functional. This is not cleanup; it is a working app vs a broken one.
2. **Same operation, same files, same moment.** It is the identical `sprints → retro-sessions` string rename, in `sprint.ts`/`report.ts`/`PreviousActionsPanel.vue` — three files Step 2 is already editing. The plan's "do the renames while frontend+backend are both being touched and before new feature code piles on" sequencing rationale (parent plan, recommended-order note) applies to these exactly as much as to the flat routes. Splitting them into a follow-up ticket re-opens the same files for the same edit later — strictly more work and more risk.
3. **Carve-out is clean.** Verified: none of the 9 nested team-routes touches `FokusSprint*`, the gRPC contract, or `GetSprintContext` (the `/api/teams` grep returned zero Fokus/context hits). The boundary the developer verified holds for this scope. No risk to the homonym carve-out.

**Required plan amendments (architect will apply on the user's yes):**
- **Step 2 scope:** add the 9 nested team-scoped callers (`sprint.ts` ×7, `report.ts` ×1, `PreviousActionsPanel.vue` ×1) → rename `/api/teams/${teamId}/sprints/${sprintId}/…` → `/api/teams/${teamId}/retro-sessions/${retroSessionId}/…`. Rename the path segment **and** the JS interpolation variable where it reads `${sprintId}` → `${retroSessionId}` for the board id (keep `${teamId}`).
- **Step 2 acceptance — add the complementary grep** (your proposed one, tightened to avoid matching the now-correct `retro-sessions` paths): `rg "/api/teams/[^/]*/sprints" client/apps/retro` → **zero**. Keep the existing `rg "/api/sprints/" … → zero`. Both must pass.
- This stays a **Refactoring** pass (no behavior change beyond making already-intended calls reach their already-existing endpoints).

**Why this is a HIGH-confidence recommendation but still a user gate:** the *technical* answer is unambiguous (broken calls, same op, clean carve-out, fix-now is cheaper). It goes to the user only because it grows the approved Step-2 scope, and scope is the owner's call (User Decision Guardrail — I don't expand owner-approved scope unilaterally). The recommendation is fold-in; the decision is the user's.

**Defer fallback (if the user says no to folding):** raise a `BUG-{N}` follow-up for "9 retro session-lifecycle frontend calls 404 against RA3 backend" — but flag to the user that this ships the merged retro app with a broken core lifecycle, which I'd advise against.

---

## Q4: Claim site count discrepancy — plan says 15 read sites, code has 16 (SprintHub alone has 4)

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 3 (SignalR + guest-token claim wire strings)
**File:** `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`

**Context:** The plan (post-critic fix L1) says: "15 read sites: SprintHub, ListCards, RetractVote, CastVote, GetMyVotes, GetParticipants, GetCurrentParticipant, GetGroups, GetPreviousActions, GetParticipantReport, CreateCard, GetSprintReport". `SprintHub.cs` alone has 4 occurrences of `FindFirstValue("sprintId")` (lines 33, 100, 180, 266). With the 1 issue site (GuestTokenAuthenticationHandler:32), the total is 17 occurrences across all files. Counting by file rather than occurrence: 1 issue site + 12 read-site files = 13 distinct locations, but actual string occurrences are 17.

The plan's "15" likely counted file-distinct named endpoints (12 endpoint files + SprintHub counted once + GetSprintReport = 14), not the 4 individual occurrences in SprintHub. In any case, the acceptance grep `rg '"sprintId"' src/Services/Reflekt` → zero remains the correct gate; the count is informational.

**Question:** Is the plan's "15 read sites" a count of endpoint files (with SprintHub counted once despite 4 occurrences)? Confirming to clarify: should the developer rename all 17 string occurrences (the correct semantic action), with the acceptance grep being the real gate?

**Recommendation:** Yes — rename all 17 occurrences. The acceptance grep (`rg '"sprintId"' src/Services/Reflekt` → zero) is the real gate; the "15" count was approximate. No plan change needed.
**Confidence:** high — the acceptance grep enforces all occurrences regardless of count.

### Answer
**RULING: Rename all 17 occurrences. The acceptance grep is the gate. APPROVED.** (architect, 2026-06-20 — confidence: high)

Verified against source — `rg '"sprintId"' src/Services/Reflekt --type cs` returns **17** string-literal occurrences across 13 files:
- 1 issue site: `GuestTokenAuthenticationHandler.cs:32` (mints the claim).
- `SprintHub.cs` ×4 (lines 33, 100, 180, 266) — the plan's "15" counted SprintHub as 1 named read site, hence the apparent undercount. All 4 read `FindFirstValue("sprintId")`.
- 12 further read sites (RetractVote, GetGroups, GetPreviousActions, GetMyVotes, ListCards, CastVote, CreateCard, GetCurrentParticipant, GetParticipants, GetParticipantReport, **GetSprintReport ×2** — one of which is the stale DO-NOT-TOUCH comment Step 3/M2 removes).

The "15 read sites" is a file-distinct named-endpoint count, not an occurrence count — informational, not a budget. The binding gate is `rg '"sprintId"' src/Services/Reflekt → zero`, which enforces every occurrence regardless of how the prose tallied them. Rename `"sprintId"` → `"retroSessionId"` at all 17.

**Note (already in Step 3, confirming it lands):** the `GetSprintReportEndpoint.cs:32` and `SetReviewStatusEndpoint.cs:12` stale `…stays sprintId-named` comments are present (verified) and are Step 3/M2's explicit removal targets — removing the comment is part of clearing the site, not a separate task. Frontend lockstep: `useSprintHub.ts` claim/payload handling per Step 3. Safe to rename — the claim is minted+read per request, no persisted/signed token carries it (critic-resolved).

**No plan change needed.** The grep gate already governs all 17.

---

## Architect rulings — summary (2026-06-20)

| Q | Ruling | Confidence | Plan change |
|---|--------|------------|-------------|
| Q1 | Proceed with all **26** flat-route DTOs | high | Typo fix: Step 2 "23"→"26" (no re-approval) |
| Q2 | Rename `VoteRepository` method → `GetParticipantIdsWithVotesForRetroSessionAsync` + call site | high | None (covered by Step 1 family rule + grep) |
| Q3 | **FOLD IN** the 9 nested team-route fixes + add complementary grep — **user scope gate** | high (recommendation) | Step 2 scope + acceptance amendment, on user yes |
| Q4 | Rename all **17** `"sprintId"` claim occurrences; grep is the gate | high | None |

**Carve-out confirmed for every blessed scope:** verified no Q1/Q2/Q3/Q4 target touches `FokusSprintSnapshot`/`FokusSprintRef`, the gRPC contract (`ISprintAnalyticsService`/`AnonymizedSprintSummary`), or `GetSprintContext*`. The `/api/teams` grep returned zero Fokus/context hits; the flat-route DTO set contains no `FokusSprint*` type. The developer's carve-out boundary holds for all four rulings.

**Q1, Q2, Q4 are settled — developer may proceed on those now.** Only Q3's scope expansion needs the user's yes before the 9-call fold-in.
