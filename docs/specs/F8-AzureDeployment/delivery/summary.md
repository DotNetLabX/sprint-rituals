# F8 — Azure Deployment — Summary
Mode: architect-led fast lane (light, ADR)
Gear: none

## Status: COMPLETE

## What Was Built
- The deploy path for the whole suite (one Host: Identity + Fokus + Reflekt + Poker) to the free F1 Linux App Service `<old-fokus-app>`: a production config file, a cross-shell deploy script and a two-phase provisioning script, a CI workflow and a manual-only deploy workflow with OIDC, and the operator runbook with the demo-day B1 procedure and the growth path. ADR-024 gained the demo-day and personal-Reflekt-app sentences.
- The live cutover (Steps 6–8: Google redirect URI, OIDC app registration, GitHub environment, `-Prepare`, the restricted cutover, the smoke test) is operator-owned and not done yet — see `docs/deployment-azure.md`.

## Key Outcomes
- 11 files: 7 created (`src/Host/appsettings.Production.json`, `deploy/deploy.ps1`, `deploy/deploy-config.example.ps1`, `deploy/provision.ps1`, `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`, `docs/deployment-azure.md`), 4 modified (`.gitignore`, `CLAUDE.md`, `docs/architecture/decisions.md` ADR-024, `docs/backlog.md` F8 row).
- Build green; `deploy.ps1 -BuildOnly` (Windows PowerShell 5.1) builds a 29.3 MB zip with all three SPAs, `Host.dll` and `appsettings.Production.json`; `-SkipBuild` and both `-SmokeOnly` checks pass under 5.1 (main session). The `pwsh` pair first runs in GitHub CI.
- **Full suite:** pass — 15 s (recorded 2026-10-30T09:03:14Z, run mvuqvnp6-f7f6da12; complete red on a fast-green tree: not measured; filtered: no) — 339 passed, 0 failed.
- **Tests and builds:** 30 s across 2 runs — complete 15 s, step runs 15 s, gate runs 0, builds 0
- **Map delta:** none
- **Risky steps:** Risky steps: Step 1 (recheck: secrets) · Unplanned: docs/backlog.md, docs/proposals/azure-deployment-handoff.md
- Done check: Check 1 PASS (Step 4 Deviated with valid reason). Code review: Codex primary NO-GO (1 HIGH, 1 MEDIUM, 2 LOW), Sonnet second reader APPROVED (risky Step 1 + skills check over every step). One fix round (cycle 1/1, light lane); no follow-up review.

## Deviations from Plan
- Step 4: the deploy job passes the app name and resource group through step `env:` instead of inline `${{ vars.… }}` — same values, no shell-injection path.
- Step 3: `-DryRun` runs no `az` command at all, reads included, so its SKU guard prints "not evaluated".
- Step 5: the runbook adds a short "Later deploys" section.

## Notes
- Fixed in round 1, not re-verified by a reader (light lane): [HIGH] CI Host gets non-secret placeholder `Google__ClientId`/`Google__ClientSecret` so the `pwsh` smoke check can redirect (`.github/workflows/ci.yml`); [MEDIUM] the cutover snapshot records subscription, RG and app and refuses another target's snapshot (`deploy/provision.ps1`); [LOW] smoke-check probes are capped to the remaining time (`deploy/deploy.ps1`, re-run by the main session: pass); [LOW] CLAUDE.md Databases table lists all four databases.
- Not verified until the operator steps: every live `az` call and its real output shape (`provision.ps1` was tested against a fake `az`), the first green GitHub CI and deploy runs, HTTPS scheme, gRPC loopback, SignalR and SQLite on `/home`, the Jira sync.
- At cutover, run `deploy.ps1` from a checkout of the merged `main` (or this worktree), never from a folder holding another feature's unsaved work — the laptop deploy ships whatever is in the working folder.
- The deploy identity cannot change the plan tier by design; demo-day B1 is the operator's own `az login` (runbook § Demo day). The B1 price is a list price, unverified.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
- **Slow tests kept:** none
- **Timed out, sent to solo:** none
- **Flaky — passed when re-run alone:** none
