# adhoc-ReflektIdentityUnification (RA2) — Critic Review

> Persisted verbatim by the team lead from the critic's transcript (agent `a5863c0b76f40a0d4`).
> The critic writes no file by design; its inline result and TaskOutput were stranded behind a
> lifecycle closer and recovered via direct JSONL extraction (subagents/agent-...jsonl, message #13).

## Mode: Plan Review (adhoc — binding inputs are ADR-016/021/022 + resolved questions Q1–Q5 + committed RA1 source `4d93287`)
## Verdict: REJECT

One CRITICAL: a second (third overall) unaddressed owner-comparison bypass site (`GetSprintSummaryEndpoint`) that produces silent incorrect output and makes the plan's own Step-3 grep-acceptance unachievable. The plan is otherwise unusually well-grounded — but this gap is the identical bug-class Q3(b) exists to fix, missed in a second location, so it must go back before implementation.

## Pre-commitment Predictions (expected vs found)
1. **SprintHub:204 line refs** — predicted mismatch. **Found accurate** (`:195` `participant?.UserId`, `:204` `NameIdentifier`, `:219` `team.OwnerId !=`). Plan's line citations are correct.
2. **ParticipantConfiguration FK + index** — predicted possible drift. **Found accurate** — FK `HasOne<User>()` SetNull at `:24`, filtered unique index `{SprintId,UserId}` at `:25`, both present; model snapshot confirms.
3. **"31 call sites" claim** — predicted possible miscount. **Found exactly 31** `GetUserId()` sites. Accurate.
4. **Migration vs snapshot** — predicted drift. **Found accurate** — `Participant→User` (SetNull) and `Team→User` (Restrict) FKs both in snapshot; drops match.
5. **Q4 mechanism feasibility** — predicted possible gap. **Found accurate** — `ActiveUserPreProcessor` reads `NameIdentifier`, early-returns at `:18-19` exactly as described; global registration at `Program.cs:52` confirmed.

**The prediction I did NOT make — and the one that matters most:** I did not predict a *second* owner-comparison helper-bypass beyond SprintHub. That is the CRITICAL finding below.

## Cross-reference Matrix

| Source requirement / ruling | Plan coverage | Status | Notes |
|---|---|---|---|
| ADR-016: remove Reflekt `User`/repo/config/Google handler | Step 1 | COVERED | But see HIGH-1 (Google/cookie registration already removed in RA1 — re-grounding error) |
| ADR-016 timing: Reflekt is `IPersonService` client in RW, not RA2 | Step note + Risks + line 19/142 | COVERED | Matches ADR-016 line 81 amendment exactly |
| ADR-021: scope `ActiveUserPreProcessor` off Reflekt | Step 4 | COVERED | Mechanism (A) correctly specified |
| ADR-021: leave SM-approval as unbuilt seam | Step 6 | COVERED | Seam preserved |
| ADR-022: `Participant.PersonId` nullable, set for authed only; no guest Person | Step 3 + Step 6 | COVERED | |
| Q1 (open + waiting room) | Step 4/6 | COVERED | |
| Q2 (no Person for guests) | Step 6 | COVERED | |
| Q3(a): all ownership keys → Person id | Step 3 | **PARTIAL** | 31 helper sites covered; `GetSprintSummary:29` owner compare NOT covered (CRITICAL-1) |
| Q3(b): repoint `SprintHub:204` to `person_id` | Step 3 | COVERED | Correct |
| Q3(c): no backfill | Step 3 + Risks | COVERED | |
| Q4: mechanism (A) assembly-of-declaring-endpoint; (C) rejected | Step 4 | COVERED | (C) correctly rejected as not-a-fallback |
| Q5: drop seed-`User`, sentinel Person-id constant | Step 7 | COVERED | |
| Step 7: drop both `HasOne<User>()` FKs + `{SprintId,UserId}` index | Step 7 | COVERED | FK names `FK_Participants_Users_UserId` / `FK_Teams_Users_OwnerId` accurate vs snapshot |

## Findings

### [CRITICAL] A second owner-comparison bypass (`GetSprintSummaryEndpoint`) is unaddressed — silent SM-status corruption, and the plan's own grep-acceptance is unachievable

**Source:** Q3 answer (plan line 20; Q3 answer in questions.md): *"The only two sites that bypass it: `SprintHub.cs:204` … and `JoinSprintResponse.UserId` (wire DTO) — both handled in Step 3."* Plan Step 3 grep-acceptance (line 79): *"`grep -rn "ClaimTypes.NameIdentifier" src/Services/Reflekt` → **zero** hits (the `SprintHub:204` bypass is gone)."*

**Issue:** There is a **third** helper-bypass site that compares to `team.OwnerId` using `ClaimTypes.NameIdentifier` directly, which the plan never names:

```csharp
// GetSprintSummaryEndpoint.cs:25-29
var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);   // reads User id
if (userIdClaim is not null && int.TryParse(userIdClaim, out var userId))
{
    var team = await teamRepository.GetByIdAsync(sprint.TeamId, ct);
    isScrumMaster = team is not null && team.OwnerId == userId;      // OwnerId is now a Person id
}
```

After Step 3, `Team.OwnerId` holds a **Person id** (written via `Team.Create(name, GetPersonId())`), but `GetSprintSummary:25` still reads the cookie's **User id** from `NameIdentifier`. The comparison `team.OwnerId == userId` therefore compares Person-id ↔ User-id and silently yields `isScrumMaster = false` for the legitimate owner. This is the **identical bug-class** Q3(b) was raised to fix for `SprintHub:204` — missed in a second location.

**Impact:** Two compounding failures:
1. **Silent incorrect output to users** — `isScrumMaster` drives SM-only affordances on the sprint summary; the real owner loses their SM badge/controls on that screen with no error.
2. **The plan's Step-3 acceptance is literally unachievable.** A developer who follows the plan to the letter — repoint `SprintHub:204`, then run `grep ClaimTypes.NameIdentifier src/Services/Reflekt` expecting zero hits — will find **≥2 remaining hits** (`GetSprintSummary:25`, `EditCard:59`) and be stranded. The "only two bypass sites" premise (plan line 20) is off by one.

**Suggestion:** Add `GetSprintSummaryEndpoint.cs:25` to Step 3's explicit edited-file list with the same treatment as `SprintHub:204` — read the `person_id` claim instead of `NameIdentifier`, so `isScrumMaster` compares Person-id ↔ Person-id. Correct the "only two sites bypass it" statement (plan line 20 + Q3 answer) to "three." Re-derive the Step-3 grep-acceptance to enumerate every `ClaimTypes.NameIdentifier` hit and its disposition.

---

### [HIGH] `EditCard:59` and `CreateCard:59` are unaddressed `NameIdentifier`/`Email` presence-checks that also defeat the Step-3 grep-acceptance

**Source:** Plan Step 3 grep-acceptance (line 79): zero `ClaimTypes.NameIdentifier` hits expected.

**Issue:** Two more cookie-claim sites the plan never names:
- `EditCardEndpoint.cs:59` — `if (User.FindFirstValue(ClaimTypes.NameIdentifier) is null)` (gate: only a cookie-authenticated suite user may edit an action item in Discussing).
- `CreateCardEndpoint.cs:59` — `if (cookieResult.Principal?.FindFirstValue(ClaimTypes.Email) is null)` (same gate, on create).

These are **presence checks** ("is there a cookie-authenticated suite user?"), not owner *comparisons*, so they do **not** produce the CRITICAL value-space mismatch — `NameIdentifier`/`Email` are still present on Identity's cookie after Step 1. So they are behaviorally lower-risk. **But** `EditCard:59` is a literal `ClaimTypes.NameIdentifier` hit, so it independently falsifies the Step-3 "zero hits" acceptance.

**Impact:** A developer cannot satisfy the stated acceptance. Worse, an over-eager developer trying to *force* zero hits might "repoint" these presence checks to `person_id` — harmless for `EditCard` (any Identity cookie carries `person_id` too after Step 2), but the plan gives no ruling, so the outcome is undefined.

**Suggestion:** Enumerate `EditCard:59` and `CreateCard:59` in Step 3 with an explicit disposition: "presence-check only — leave as-is (still valid: Identity cookie carries these), OR repoint to `person_id` for uniformity." Either is fine; the plan must *state* which, and the grep-acceptance must account for them (e.g. "zero `NameIdentifier` *owner-comparison* sites" with an allow-list for the two presence checks, mirroring the sanctioned `JoinSprintResponse.UserId` carve-out).

---

### [HIGH] Step 1 re-grounding error: the cookie/Google registration it tells you to delete was already removed in committed RA1

**Source:** Plan Step 1 (line 40): *"`AuthenticationConfiguration.cs` cookie + Google registration and `OnGoogleTicketReceived` (RA1 already stopped calling them; RA2 deletes them)."* Plan line 6 claims it is *"re-grounded against committed RA1."*

**Issue:** Committed RA1 `AuthenticationConfiguration.cs` (HEAD `4d93287`) contains **only**:
```csharp
services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>(
        GuestTokenDefaults.AuthenticationScheme, _ => { });
```
There is **no `AddCookie`, no `AddGoogle`, and no `OnGoogleTicketReceived`** left to delete — the file's own header comment says *"OnGoogleTicketReceived (writes Reflekt User) is removed — RA1 work (ADR-016)."* RA1 did not merely "stop calling them"; it deleted them. The plan's Step-1 deletion target here does not exist.

**Impact:** A developer follows Step 1, opens `AuthenticationConfiguration.cs` to delete cookie/Google registration, finds nothing to delete, and is left unsure whether the file is correct or the plan is stale — a stall and a credibility hit.

**Suggestion:** Correct Step 1: `AuthenticationConfiguration.cs` should be **kept as-is** (it already adds only the guest scheme — exactly the RA2 target). Remove "cookie + Google registration and `OnGoogleTicketReceived`" from the deletion list. The accurate Step-1 deletions are: `User` aggregate/repo/config, `Users` DbSet, the seed-`User` block, the two `HasOne<User>()` configs, and the four already-stubbed `_ra1/unmapped` endpoints.

---

### [MEDIUM] Dead code `GetByUserAndSprintAsync` and `JoinWithGoogle` reference `UserId` but aren't in any Step-3 edit/grep target

**Source:** Plan Step 3 edited-file list (lines 71–75) and grep-acceptance (line 79).

**Issue:** Two members still reference the old `UserId` and have **zero live callers** (grep-confirmed):
- `ParticipantRepository.GetByUserAndSprintAsync(int userId, …)` at `:22` — body `p.UserId == userId` at `:24`. The plan names *only* `:24` (the filter), not the method's `int userId` parameter or that the method is dead.
- `Participant.JoinWithGoogle(…, int userId)` at `Behaviors/Participant.cs:20` — the plan names *only* `:30` (the `UserId = userId` init), implying it treats `JoinWithGoogle` as a live rename target when it is dead and unreferenced.

**Impact:** Low blast radius (both are dead), but they will still appear in a `\.UserId` grep and break the "zero `.UserId`" spirit of the acceptance unless renamed/removed.

**Suggestion:** In Step 3, note that `GetByUserAndSprintAsync` and `JoinWithGoogle` are dead; either delete them (cleanest, since Step 6 authenticated-join can set `PersonId` inline or via a renamed `JoinWithPerson`) or rename their params/bodies to `personId`/`PersonId`. State which, so the grep-acceptance is satisfiable.

---

### [MEDIUM] Step 6 doesn't name the existing authenticated-join factory (`JoinWithGoogle`) it must replace

**Source:** Plan Step 6 (line 102): *"Authenticated join … set `Participant.PersonId = GetPersonId()`."*

**Issue:** RA1's only authenticated-join construction path is `Participant.JoinWithGoogle(displayName, sprintId, userId)` (`Behaviors/Participant.cs:20`), which sets `UserId`. The plan's Step 6 describes the *behavior* (set `PersonId` on authed join) but never connects it to this factory, nor states whether the authed-join endpoint exists. The only mapped join endpoint in committed RA1 is `JoinSprintEndpoint` (guest-only, `Participant.Join(...)`); `JoinSprintGoogleEndpoint` is the stubbed `_ra1/unmapped` placeholder the plan correctly deletes in Step 1. **So after Step 1 there is no authenticated-join endpoint at all.**

**Impact:** Ambiguity: a developer reading Step 6 may not realize that "set `PersonId` on authenticated join" has no surviving endpoint to attach to, and that the authed-join path is therefore effectively dormant in RA2. Two developers could implement this differently.

**Suggestion:** Step 6 should state the concrete shape: with `JoinSprintGoogleEndpoint` deleted (Step 1), how does a suite member join with `PersonId` set in RA2 — via `JoinSprintEndpoint` reading the optional `person_id` claim, or is authed-join deferred to RW with only the domain factory (`JoinWithPerson`) prepared? Make the RA2 endpoint surface explicit rather than describe behavior with no endpoint.

---

### [LOW] Report endpoints use `GetUserId()` — covered, noted for completeness

No action needed. All `team.OwnerId != userId` comparison sites where `userId = User.GetUserId()` (28 of them) flip correctly and consistently when the helper is repointed — the plan's core Q3 claim holds for these. Confirmed by grep. Listed only to document that the cross-reference was exhaustive, not sampled.

## Gap Analysis

- **What's missing (not just wrong):** The plan's value-space analysis (Q3) found bypasses by *inspection* (it spotted SprintHub and the DTO) but never ran the exhaustive `grep ClaimTypes.NameIdentifier` that it nonetheless codifies as the *acceptance criterion*. Result: it asserts "two bypass sites" / "zero grep hits" when the real counts are three owner-relevant sites and ≥4 grep hits. The acceptance criterion is stronger than the scope — a self-inconsistency.
- **Edge case probed:** *Owner opens the sprint summary screen after RA2.* Input: suite member who is the team owner, authenticated via Identity cookie (`person_id` = P, `NameIdentifier` = U, P≠U). Expected: `isScrumMaster = true`. Actual under the plan as written: `GetSprintSummary:29` computes `team.OwnerId (=P) == userId (=U)` → **false**. SM controls vanish silently. This is the concrete scenario that makes CRITICAL-1 user-visible.
- **Backward impact:** The plan correctly identifies that the value-space flip is cross-cutting and that the single helper absorbs 31 sites. It missed that **two non-helper read paths** (`GetSprintSummary`, and the already-flagged `SprintHub`) participate in ownership comparison. SprintHub was caught; GetSprintSummary was not. The systemic lesson: any `ClaimTypes.NameIdentifier`-read that flows into a `team.OwnerId` comparison is a value-space-flip site, and the plan should derive that set from grep, not from spot inspection.

## Adversarial-mode note
The systemic pattern is **not** rushing or copy-without-understanding — the plan is genuinely well-grounded (line refs, FK names, the 31-count, the migration drops, the Q4 mechanism are all verified-accurate against committed source, which is rare). The single systemic flaw is narrow: **an acceptance criterion (`grep → zero NameIdentifier hits`) was written from spot-inspection of bypass sites rather than from the exhaustive grep it prescribes**, so it under-counted by one owner-comparison site (CRITICAL-1) and several presence-check sites (HIGH-2). Fixing the plan is mechanical: run the grep, enumerate every hit with a disposition, and add `GetSprintSummary:25` to the Step-3 repoint list. After that, this plan is in good shape for implementation.

## Open Questions
- None requiring confidence downgrade — all findings carry direct file:line evidence. CRITICAL-1 and the value-space reasoning are HIGH confidence; HIGH-1 re-grounding error is HIGH confidence with quoted file evidence.

```
Reviewed: docs/specs/adhoc-ReflektIdentityUnification/delivery/plan.md
```
