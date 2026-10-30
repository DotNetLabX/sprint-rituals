# F8-AzureDeployment — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-30 — after the build

**Model:** claude-opus-5-5

Pre-commitment predictions: (1) Step 2's script runs left unproven because the developer seat cannot run them — held, and discharged by the main session's recorded runs; (2) a Step 4 departure on how the workflow passes the target — held, documented; (3) the ADR-024 amendment spilling past its two sentences — did not happen.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Production configuration file | Implemented | Re-verified: file holds only `Kestrel` + `ConnectionStrings`, `Http.Url=http://+:8080`, no `Grpc` key, the four `/home/data/sprint-rituals/*.db` values; `git diff --stat 4affe6a` of `appsettings.json` + `appsettings.Development.json` empty. Publish result recorded in the step block. |
| 2 — Deploy script | Implemented | Re-verified: target-name grep = 0; every native call followed by `Assert-LastExitCode` (curl exit captured as retry); `.gitignore` entries confirmed by `git check-ignore`. Command acceptances (`-BuildOnly` zip listing, `-SkipBuild` missing-artifact exit 1, `-SmokeOnly` pass/fail pair) recorded by the main session under Step 2. The `pwsh` pair exists as `ci.yml` steps; its first live run is operator Step 8, as Step 4's acceptance states. |
| 3 — Provisioning script (two phases) | Implemented | Re-verified: `--sku`/plan-update grep = 0; the 5 settings/connection-string hits each carry `--query` or `-o none`; secret-assignment grep = 0; `Invoke-AzWrite` used only in `Invoke-Cutover`. DryRun and checksum results in the step block. `-DryRun` running no `az` call at all is a stricter reading of "changes nothing", recorded as a decision. |
| 4 — GitHub workflows | Deviated (valid reason) | App name / resource group reach `deploy.ps1` through step `env:` instead of inline `${{ vars.… }}` in `run:` — same values, removes a script-injection path. Re-verified: `on:` = `workflow_dispatch` only; `id-token: write` once, line 51 under `deploy:` (line 45); `npm`/`dotnet` hits lines 30, 33 inside `build`; `secrets.` = 0; every `uses:` pinned to a 40-hex SHA with a version comment; Reflekt grep = 0. |
| 5 — Runbook, ADR-024 amendment and repo pointers | Implemented | Re-verified: all 17 runbook terms ≥ 1; both ADR sentences `grep -c` = 1; `CLAUDE.md` has the production-path note and `## Deployment`. Extra "Later deploys" runbook section carries no new decision. |
| 6 — Prepare (Owner: operator) | N/A (operator-owned) | Live Azure / Google / GitHub actions. |
| 7 — Cutover (Owner: operator) | N/A (operator-owned) | Live cutover. |
| 8 — Smoke test (Owner: operator) | N/A (operator-owned) | Includes the first green `ci.yml` run (the `pwsh` smoke pair). |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list — plan header `not applicable`).

Notes:
- Skill log: every step is `Skill: None`, `TDD: no` (Skills per step: on, nothing mapped) — the all-`None` exemption applies; the session-scoped window (both homes, from 08:31:59Z) holds no developer skill line, as expected. `## Skills Used` is present.
- Test-entry log: the window's developer `test-entry` lines are two `blocked: true` refusals (08:36:06Z, 08:36:23Z — the Step 2 script runs, later run by the main session) and three `script-unread` disclosures — no unblocked line, so no test slip.
- Light lane, full-format plan: every acceptance is a grep or command; each has its result in the step block. No test-file acceptance, so no step-run record is owed.
- Carry-over (low, pre-existing, not a Fail): the `CLAUDE.md` `## Databases` table still lists two of the Host's four databases — a backlog-sized doc fix.
- Plan hygiene: `## Decisions` and `## Decisions and why` present and filled.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-30*
