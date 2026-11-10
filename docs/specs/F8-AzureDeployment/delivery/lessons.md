# F8-AzureDeployment — Lessons

## Architect Lessons

- **Slug number derived from a bad grep, presented as High.** I proposed `F7` with "Confidence: high — F6 is the last number" from a `grep -o | sort -n` that sorted wrongly; another session had claimed F7 the same day. The owner approved, the fact was wrong, and the ask was re-presented (F8). Fix: before proposing a slug, read the backlog's **tables** in the same turn (multi-session file) and `ls docs/specs/` — never a numeric sort over grep tokens. [F8-AzureDeployment]
- **Owner closed the ideas stage with nothing chosen.** "Got your ideas, move to interview" left every idea parked; the critique turned into a prose comparison followed by a question box. That worked — the pick happened in the interview. [F8-AzureDeployment]
- **Check how a ported pattern behaves on the new platform before keeping it.** The proposal carried over "SQLite on `/home`" from old Fokus as proven. Reading the local `.db` headers (bytes 18–19 = WAL) and SQLite's network-filesystem limitation showed that assumption doesn't hold. Before reusing a "proven" pattern, check what it depends on. [F8-AzureDeployment]

- **Test the binding constraint before pricing options.** I recommended a paid plan (B1, then Postgres) without asking whether zero cost was binding. It was the reason the free plan was chosen in the first place, and it ruled out every option I had priced. When the source system runs on a free tier, ask "must this stay free?" before recommending an upgrade. [F8-AzureDeployment]
- **The live system is evidence too.** Both live apps have run SQLite on App Service `/home` on F1 without trouble. I weighed Microsoft's general warning above what the owner's apps have actually shown, and the owner's usage profile (a few users, no simultaneous writes) settled it. Report the vendor warning, but weigh it against the observed setup. [F8-AzureDeployment]
- Done check (F8): a plan whose acceptances are script runs (`powershell.exe -File deploy/*.ps1 …`) can't be met on the developer seat, because the test-entry gate blocks them. The plan should name the main session as the one who runs those commands up front, the way an operator-owned step is named. That makes the hand-off planned, not a Test problem the developer finds mid-build. **Evidence:** [F8-AzureDeployment]

## Skill Gaps

### hosting-environment-config
- **Kind:** missing
- **Searched for:** where production-only, non-secret config lives (environment JSON vs platform settings) and how explicit Kestrel endpoints interact with platform ports
- **Why it would help:** would fix the `appsettings.{Environment}.json` + secrets-in-platform split; planned inline instead (Step 1)
- **References:** `src/Host/appsettings.json`, `src/Host/appsettings.Production.json` (to be created)
- **Evidence:** [F8-AzureDeployment]

### deploy-script-and-pipeline
- **Kind:** missing
- **Searched for:** authoring a cross-platform build→publish→zip→deploy script, an idempotent provisioning script, and GitHub CI + manual-deploy workflows with OIDC
- **Why it would help:** three plan steps (3–5) went `Skill: None` with inline contracts
- **References:** `<fokus-repo>/deploy.ps1`, `<reflekt-repo>/deploy.ps1`, `deploy/` + `.github/workflows/` (to be created)
- **Evidence:** [F8-AzureDeployment]

- **A last-round NO-GO reads as "still failing" to the owner.** Round 2 ended NO-GO, every finding was folded, and the schedule (no CRITICAL → no round 3) closed the review — but the plan's review note didn't say the verdict was on the pre-fold text, so the owner came back believing findings were open. Fix: when a review ends on folded findings with no verifying round, the `## Plan Review` note says in one line that the verdict judged the pre-fold plan and where each fold landed. [F8-AzureDeployment, 2026-10-30]
- **Check how a limit actually bites before pricing the workaround.** The owner framed the demo risk as F1's 5 WebSocket connections; reading the three hub clients showed default transports, so connections past five fall back and keep working. The real demo-stopper is the 60 CPU-minute allowance (the site stops when it runs out). [F8-AzureDeployment, 2026-10-30]

## Developer Lessons

- **The test-entry gate refuses plan-mandated script acceptances that build.** Step 2's acceptance is `powershell.exe -File deploy/deploy.ps1 -BuildOnly` / `-SkipBuild` / `-SmokeOnly`; the Nexus test-entry gate blocks any command naming `deploy/deploy.ps1` ("runs a build or the test runner itself") — even `-SkipBuild` with no artifact, which builds nothing — while the step run reports `skipped: nothing to run` for a non-.NET change. A developer cannot prove such a step; the main session ran it. Fix: a plan whose acceptance runs a build script should mark that check `Owner: close` (main session) up front, or the gate should allow a plan-named acceptance command. [F8-AzureDeployment]
- **Exercise an `az` script with no Azure: a fake `az.cmd` first on `PATH`.** A failing fake (`@exit /b 99`) proves a `-DryRun` calls nothing; a fake that forwards to a small Node script returning canned output per subcommand lets every branch (SKU guard, write-once snapshot, post-condition) run locally, with a call log to show what would have been sent. [F8-AzureDeployment]
- **A substring grep for `set` cannot prove an `az` code path read-only.** `appsettings list` contains `set`; the Step 3 check needed a word grep (`grep -wE "set|delete|update"`) over the read-only functions. Write such acceptances as word greps, or name the forbidden subcommands. [F8-AzureDeployment]
- **A CI step that starts the Host must supply every option the Host validates — the laptop's user secrets hide the gap.** The smoke check passed locally only because the dev machine had Google credentials; on a clean runner the empty `Google:ClientId`/`ClientSecret` in `appsettings.json` fail the Google handler's options validation on every request. Give such a step non-secret placeholder `env:` values for each validated option (here `Google__ClientId`, `Google__ClientSecret`; Jira already has placeholders in `appsettings.json`). [F8-AzureDeployment]

- **Plan acceptances the developer seat cannot run must name their runner.** Step 2's `deploy.ps1` runs were refused by the test-entry gate on the developer seat; the coordinator ran them. A plan whose Accept lines run a build script should say "run by the main session" up front, so the developer doesn't spend attempts on them. [F8-AzureDeployment, 2026-10-30, architect]
- **A laptop smoke pass hid a CI-only start-up failure.** The local Host had Google values from user secrets; a clean runner has none, and the Google handler's option check runs on every request. Only Codex's code-grounded read caught it. For any CI step that starts the app, list every setting validated at start-up and supply non-secret placeholders. [F8-AzureDeployment, 2026-10-30, architect]
