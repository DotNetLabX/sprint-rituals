# F8-AzureDeployment — Review

## Step 2 — Risky steps

Risky steps: Step 1 (recheck: secrets) · Unplanned: docs/backlog.md, docs/proposals/azure-deployment-handoff.md

(Recheck run 2026-10-30 by the architect, `risk-recheck.js --base bdb682f`. The two unplanned files are the feature's own planning artifacts carried into the worktree, not built code.)

## Step 2 — Code Review

**Verdict: REQUEST CHANGES**
Primary: Codex — NO-GO, persisted by the standalone architect (session b1593ba1-a310-4ac9-897f-6db5c9a10fb8)
**Model:** codex
Run record: handover tied run mvuqbkoe-522d52f7 — pass; deploy.ps1 command checks under implementation.md Step 2 — pass (PowerShell 5.1).

### [HIGH] CI smoke check cannot pass — the Host starts without Google options on a clean runner
`.github/workflows/ci.yml:39`. Both checked-in Google credentials are empty; every request initializes the Google remote handler, whose options validation rejects empty `ClientId`/`ClientSecret`, so `/` never answers the expected redirect on the runner (it passed locally only because the laptop has the values). Fix: give the CI Host step explicit non-secret dummy `Google__ClientId` / `Google__ClientSecret` environment values, then the two `pwsh` smoke runs can pass.

### [MEDIUM] Cutover snapshot reuse checks only that the file exists
`deploy/provision.ps1:166`. After the target changes (another app, RG or subscription), another target's `old-config.txt` is accepted as a resumed cutover and the new target's startup command and `Database` path are never saved. Fix: record the target (subscription, RG, app) in the snapshot and refuse any write when an existing snapshot names a different target; keep the existing snapshot untouched on mismatch.

### [LOW] Smoke-check timeout can be exceeded and still report success
`deploy/deploy.ps1:155`. Each probe uses a fixed 20 s curl timeout and a success skips the deadline check. Fix: cap each probe's `--max-time` at the remaining time and check the deadline before accepting a success.

### [LOW] CLAUDE.md database table describes two of four databases (pre-existing)
`CLAUDE.md:77`. Fix: add `reflekt.db` (`ReflektDatabase`, 3rd) and `poker.db` (`PokerDatabase`, 4th) and update the "Both databases" sentence.

*Status: COMPLETE — Codex primary, persisted by architect, 2026-10-30*

## Step 2 — Fix list, cycle 1

Readers in: Codex (primary, NO-GO), Sonnet second reader (APPROVED — nothing at the bar, `review-second.md`), done check (PASS, `done-check.md` Check 1). Light lane: one fix round, carrying every item at any level.

**From the code review (Codex):**
1. [HIGH] `.github/workflows/ci.yml` — give the CI Host step non-secret dummy `Google__ClientId` and `Google__ClientSecret` env values so `/` can redirect; keep `secrets.` grep at 0 and add no `id-token`.
2. [MEDIUM] `deploy/provision.ps1` — write the target (subscription, resource group, app name) into `deploy/.cutover/old-config.txt`; on an existing snapshot naming a different target, refuse before any write and leave the file byte-identical.
3. [LOW] `deploy/deploy.ps1` — cap each smoke probe's curl timeout at the remaining time and check the deadline before accepting a success.
4. [LOW] `CLAUDE.md` — Databases table: add `reflekt.db` / `ReflektDatabase` (3rd) and `poker.db` / `PokerDatabase` (4th); fix the "Both databases" sentence.

Follow-up review: none due — light lane (fixes go to summary.md as "fixed, not re-verified").
