# Nexus plugin feedback — v1.18.6 — 2026-06-29

Portable feedback file (ADR-1). These items are **plugin-bound**: they target nexus shipped hooks
and skills that live in the version-keyed plugin cache and cannot be edited from this consuming
project. Send this file to the plugin maintainer; on the maintainer's machine the same entries are
applied directly in the plugin source repo (`<plugin-repo>/plugins`).

Source project: **sprint-rituals**. Origin: learner consolidation pass, 2026-06-29 — across the
Fokus + Reflekt feature/adhoc pipeline runs. Both items below cleared the 2-occurrence promotion
threshold and were **verified still-present against the live plugin source (nexus 1.18.6 /
nexus-dotnet 1.2.0)** before routing — they are not stale-against-a-since-fixed HEAD.

**Status:** READY for maintainer. Not applied locally (ADR-1).

---

## Custom spawn-name suffix defeats agent_type / owner matching in two hooks

- **Suggested target:** `nexus/hooks/scripts/verify-gate.js` (role normalization, ~line 115) **and**
  `nexus/hooks/scripts/boundary-detector.js` (role normalization, ~line 77). Same root cause, two
  call sites — fix both, ideally via one shared helper in `hooks/scripts/lib/`.
- **Action:** update (deterministic check fix — preferred over any prose mitigation).
- **Evidence:** `F6-PlanningPoker`, `adhoc-ReflektFrontendMerge`, `adhoc-ReflektComputationToDomain`,
  `adhoc-ReflektValueObjects`, `adhoc-SprintContextPanel` — recurrence **5x** (plus two standing
  consumer-side memory notes: "verify-gate skips on custom spawn names", "boundary-detector
  false-positive on named agents"). The original F6 lessons proposal is the formal write-up.
- **Lesson:** Both hooks normalize the agent role with `String(data.agent_type).toLowerCase().split(/[:/]/).pop()`.
  That strips a **namespace** prefix (`nexus:developer` → `developer`) but **not** the team's
  custom spawn-name suffix convention (`developer-f6`, `architect-rrc`, `dev-ra5a`, `developer-rrb`),
  because those use a hyphen, not `:`/`/`. Consequences observed every run that names its spawns:
  - `verify-gate.js`: `developer-f6` ∉ `IMPL_ROLES` → Branch 3 writes
    `verdict:"skipped", reason:"unrecognized agent_type: developer-f6", blocking_failed:false`.
    `blocking_failed:false` **reads as "passed" but means "never ran"** — the verify set silently
    does not execute, so the only real gate becomes the architect done-check re-running builds by hand.
  - `boundary-detector.js`: `role` stays `developer-f6`, so `ARTIFACT_OWNERS` `owners.has(role)` is
    false for a developer writing its **own** `implementation.md` → false-positive ADR-18
    owner-violation (likewise `architect-rrc`→`plan.md`, `reviewer-rrc`→`review.md`). Noise that
    every run must hand-adjudicate "strip suffix, compare base role" to avoid a wrongful void.
  - **Fix:** after the `:`/`/` split, also resolve the token to its base pipeline role by matching
    against the known-role set as a prefix — `developer-f6` → `developer`. **Landmine:** do **not**
    naïvely strip everything after the first hyphen — `team-lead` is itself a hyphenated base role
    and would collapse to `team`. Try the full token against the role set first; only then strip a
    trailing `-<suffix>` segment when the remaining prefix is a known role. The role sets already
    exist in both files (`IMPL_ROLES`/`NONIMPL_ROLES`; `PIPELINE_ROLES`/`NONCODE_ROLES`).

## Skill gap: `group-scalars-into-value-object` (flat scalars → VO refactor, end to end)

- **Suggested target:** new skill `nexus-dotnet/skills/group-scalars-into-value-object/` (refactoring
  family, sibling to `domain-patterns` / `create-aggregate`). Confirmed absent in live nexus-dotnet
  1.2.0; `domain-patterns` and `persistence-patterns` mention `ComplexProperty` but cover no
  end-to-end refactor.
- **Action:** add (new skill).
- **Evidence:** `adhoc-Pass3b-AppSettingsVOs` (Step 4 disposition `None`) and
  `adhoc-ReflektValueObjects` / RR-B (Step 4 disposition `None`) — recurrence **2x**, same gap shape
  across two services (Fokus + Reflekt), each hitting the same hazards. **Prior remedy already
  tried:** Pass3b's consolidation chose to *fold* partial guidance into `persistence-patterns` +
  `create-feature` Mappings (owner decision) rather than create a skill — and RR-B Step 4 still
  came up `None` anyway. The fold did not close the gap; that recurrence-after-fold is the basis for
  now proposing a dedicated skill rather than re-folding.
- **Lesson:** Codify the flat-scalars→VO refactor as one procedure:
  1. Extract the VO by **composing existing single-value VOs** — don't duplicate their guards.
  2. The `required init` → `private set` accessor flip when a `required` member becomes a VO member
     (pre-empt CS9032).
  3. EF `ComplexProperty` with a **rename-not-drop** migration (the `{Prop}_{Member}` column-name
     rule), not a drop+recreate.
  4. A read-site sweep driven by **one authoritative grep that is also the step's acceptance check**,
     explicitly including **serialized payloads** (SignalR/JSON) whose camelCase keys a PascalCase
     property grep silently won't match — a missed SignalR consumer was the critic catch in RR-B.
  5. Explicit Mapster `.Map` rules at the wire-flat boundary, because the regrouped source paths no
     longer line up with the flat destination names.

---

## Cross-reference

- Local source lessons (tagged `[ROUTED-TO-PLUGIN]`):
  `docs/specs/F6-PlanningPoker/delivery/lessons.md`,
  `docs/specs/adhoc-ReflektValueObjects/delivery/lessons.md`.
- Not in this file — already resolved upstream, `[APPLIED]`: the APPROVED-verdict gate's lexical
  `CRITICAL/HIGH` false-positive on `Confidence: HIGH` qualifiers (2 occurrences,
  `adhoc-ReflektLiftAndShift` RA1 + `adhoc-ReflektIdentityUnification` RA2 — both pre-date the fix).
  `pipeline-gate.js` `approvedWithOpenHighSev` now anchors structurally on the `### [CRITICAL|HIGH]`
  finding-heading shape; shipped via plugin-feedback **nexus-1.13.0 item 1**. Not re-routed (stale).
- Not in this file (kept `[TRACKED]` in source, 1 occurrence — awaiting a 2nd before routing):
  the `pipeline-gate.js` two-repo blind spot (`adhoc-Pass1-SkillRepair`, already flagged to the
  owner by the 2026-06-08 consolidation).
- The prior feedback file `nexus-1.16.1-2026-06-21.md` (ADR-023 doc-structure P1–P5) is a separate,
  still-open track — not superseded by this file.

---

## Maintainer disposition — applied in the nexus source repo, 2026-06-29 (→ nexus 1.18.7)

Consolidated by the **learner** on the maintainer's machine (the plugin source repo, ADR-1), alongside
the knowledge-gateway file of the same date.

| Item | Disposition | Where (nexus 1.18.7) |
|------|-------------|----------------------|
| Custom spawn-name suffix defeats `agent_type`/owner matching in two hooks | **[APPLIED]** — exactly as proposed: a single shared helper `hooks/scripts/lib/resolve-role.js` resolves the base role for **both** `verify-gate.js` and `boundary-detector.js`, full-token-first then longest-prefix peel (the `team-lead`→`team` landmine you flagged is handled and tested). Merged with knowledge-gateway P1 — combined ~27 occurrences. | `lib/resolve-role.js` + both hooks + `tests/unit/resolve-role.test.mjs` |
| Skill gap: `group-scalars-into-value-object` (nexus-dotnet) | **[DEFERRED — separate pass]** — confirmed a genuine 2× gap (not stale); authoring a full nexus-dotnet skill (scaffold + lint gate via `improve-skills`) is its own piece of work, scheduled as the next learner pass rather than folded into this hook/prose batch. Still tracked. | (pending) |
