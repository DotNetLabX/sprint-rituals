# F8 — Azure Deployment — Implementation

**Model:** claude-opus-5-5

## Step 1 — Production configuration file
**Files:** `src/Host/appsettings.Production.json` — new; re-pins the public Kestrel `Http` endpoint to `http://+:8080` (keeps `Http1AndHttp2`; `Grpc` endpoint and `Grpc:Services:*` stay inherited from `appsettings.json`) and points the four connection strings at `/home/data/sprint-rituals/{identity|fokus|reflekt|poker}.db`. No secrets, no return origins.
**Skills:** None — plan Skill Mapping row 1 `(none)`, TDD no
**Decisions:** None
**Deviations:** None
Acceptance (results):
- `ConvertFrom-Json` (Windows PowerShell 5.1): `Http.Url=http://+:8080`; `Grpc` key absent under `Kestrel.Endpoints`; `ConnectionStrings` = exactly `IdentityDatabase`, `Database`, `ReflektDatabase`, `PokerDatabase` with the four planned values; top-level keys = `Kestrel,ConnectionStrings`.
- `git diff --stat 4affe6a -- src/Host/appsettings.json src/Host/appsettings.Development.json` — empty (no non-F8 commit touched either file).
- `dotnet publish src/Host -c Release -o artifacts/publish` — succeeded; output contains `appsettings.Production.json` and `Host.dll`.
- Step run attempt 1 (build only, run `mvupt1op-0280a77b`) — pass.
- Not verified locally: that App Service sets `ASPNETCORE_ENVIRONMENT=Production` and that `/home` is writable — operator Step 8.

## Step 2 — Deploy script
**Files:** `deploy/deploy.ps1` — new; one script for Windows PowerShell 5.1 and PowerShell 7: target from `-AppName`/`-ResourceGroup`/`-Subscription` or `deploy/deploy-config.ps1`, modes default / `-BuildOnly` / `-SkipBuild` (zip and in-zip `Host.dll` checked before any `az` call or target lookup) / `-SmokeOnly -BaseUrl`; build = `npm ci` + `npm run build` in `client`, `dotnet publish src/Host -c Release -o artifacts/publish`, zip via `System.IO.Compression` with forward-slash entry names; deploy = `az webapp deploy --type zip --clean true --track-status false --only-show-errors -o none`; smoke check polls `{BaseUrl}/` with `curl.exe` (Windows) / `curl` (elsewhere), never following redirects, until a 3xx whose location ends in `/dashboard/login`, retrying until `-TimeoutSeconds` (default 300); `$ErrorActionPreference = 'Stop'`, an exit-code check after every native call, exit 1 on any failure.
`deploy/deploy-config.example.ps1` — new; `$DeployAppName = '<old-fokus-app>'`, `$DeployResourceGroup = '<resource-group>'`, optional `$DeploySubscription`; the only place those names live.
`.gitignore` — adds `deploy/deploy-config.ps1`, `deploy/.cutover/`, `artifacts/` (confirmed with `git check-ignore -v`).
**Skills:** None — plan Skill Mapping row 2 `(none)`, TDD no
**Decisions:** Config variables are named `$Deploy*` and dot-sourced inside a child scope, so the config file never overwrites a passed parameter; `-SkipBuild` checks the artifact before resolving the target, so a missing zip fails with the artifact message even with no config; at most one of `-BuildOnly`/`-SkipBuild`/`-SmokeOnly`; `--only-show-errors` added to `az webapp deploy` so CLI warnings do not reach output; no comment-based help block (runbook documents usage).
**Deviations:** None
**Test problem:** `powershell.exe -File deploy/deploy.ps1 -BuildOnly` and `-SkipBuild` (artifacts deleted) — refused by the Nexus test-entry gate ("runs a build or the test runner itself") — not fixed — tried: running each once; owed to the main session (zip listing for `wwwroot/{dashboard,retro,poker}/index.html`, `Host.dll`, `appsettings.Production.json`, no `\`; `-SkipBuild` non-zero with the missing-artifact message).
**Test problem:** `-SmokeOnly -BaseUrl http://localhost:5100` against a running Host, and against an unused port with `-TimeoutSeconds 10` — not run (needs `dotnet run` + the script, both gate-refused) — owed to the main session; the `pwsh` pair runs in `ci.yml` (Step 4) on the runner — not verified locally.
**Test problem:** Step run attempt 1 (run `mvupxmtq-d2285921`) — `skipped`, "nothing to run" (no .NET file changed) — not fixed — nothing to fix.
Acceptance (results):
- `grep -c "<old-fokus-app>\|<resource-group>" deploy/deploy.ps1` = 0.
- Every native call is followed by an exit-code check (read and confirmed): `npm ci`→`Assert-LastExitCode`, `npm run build`→`Assert-LastExitCode`, `dotnet publish`→`Assert-LastExitCode`, `az webapp deploy`→`Assert-LastExitCode`; the `curl` probe captures `$LASTEXITCODE` and treats non-zero as a transient retry.
- Windows PowerShell 5.1 parser: 0 parse errors in `deploy/deploy.ps1`.
- Probe (no build): `New-DeployZip` extracted from the script and run under 5.1 on a scratch folder (`Host.dll`, `wwwroot/dashboard/index.html`) → entries `Host.dll`, `wwwroot/dashboard/index.html` — forward slashes on 5.1.
- Not verified locally: any `az` call (live Azure), PowerShell 7 behaviour (`pwsh` not installed here — CI runner).

**Main-session command checks (architect, coordinator, 2026-10-30, Windows PowerShell 5.1, worktree the worktree) — discharges the two Step 2 test problems above, except the `pwsh` pair:**
- `powershell.exe -File deploy/deploy.ps1 -BuildOnly` → exit 0; `artifacts/deploy.zip` 29.3 MB, 259 entries; contains `wwwroot/dashboard/index.html`, `wwwroot/retro/index.html`, `wwwroot/poker/index.html`, `Host.dll`, `appsettings.Production.json`; 0 entries contain `\`. — pass
- `-SkipBuild` with `artifacts/` moved away → exit 1, `ERROR: Missing deploy artifact …\artifacts\deploy.zip - run with -BuildOnly first`, no `az` call. — pass
- Host running locally (`dotnet run --project src/Host`): `-SmokeOnly -BaseUrl http://localhost:5100` → exit 0 (`302 -> /dashboard/login`); `-SmokeOnly -BaseUrl http://localhost:5199 -TimeoutSeconds 10` → exit 1 after retries (curl exit 7). — pass
- The same two `-SmokeOnly` runs under `pwsh`: not verified here — the first `ci.yml` run (Step 8, operator).

## Step 3 — Provisioning script (two phases)
**Files:** `deploy/provision.ps1` — new; same target resolution as `deploy.ps1` (parameters, else `deploy/deploy-config.ps1`), 5.1/7-compatible, exactly one of `-Prepare`/`-Cutover`, plus `-DryRun`. Both phases run the zero-cost guard (plan SKU must be `F1` or tier `Free`, else exit 1; the script never changes a plan or SKU). `-Prepare` is read-only: plan name, the names of every app on that plan (`webapp list` filtered by plan id, case-insensitive), missing required (`Google__ClientId`, `Google__ClientSecret`, `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey`) and optional (`Xray__ClientId`, `Xray__ClientSecret`, `Xray__BaseUrl`) settings by name, current startup command, current `Database` path. `-Cutover`: write-once snapshot to `deploy/.cutover/old-config.txt` (app, RG, UTC time, startup command, `Database` path; refuses when no snapshot exists and the startup command is already `dotnet Host.dll`; an existing file is kept untouched), then the planned writes (app settings `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, `SCM_DO_BUILD_DURING_DEPLOYMENT`, `Auth__AllowedReturnOrigins__0=https://{AppName}.azurewebsites.net`; container logging to filesystem; HTTP/2; startup `dotnet Host.dll`; HTTPS-only; delete the `Database` connection string), then the post-condition (no `Database` name, else exit 1) and the note that the old database file is not touched. Reads go through `Invoke-AzRead` (always `--query`, names or a single value), writes through `Invoke-AzWrite` (always `-o none`); every call adds `--only-show-errors` and checks the exit code.
**Skills:** None — plan Skill Mapping row 3 `(none)`, TDD no
**Decisions:** `-DryRun` runs **no** `az` command at all, reads included — it prints every command (the SKU guard prints "not evaluated"), so a dry run never needs or touches Azure; the plan's "changes nothing" is met strictly. Subcommand and `-o none` sit in one string per call so the grep acceptances read literal lines. Snapshot also records app, RG and time, for the rollback reader.
**Deviations:** None
**Test problem:** Step run attempt 1 (run `mvuq2hq4-3c8b1adb`) — `skipped`, "nothing to run" (no .NET file changed) — not fixed — nothing to fix.
Acceptance (results):
- `-Cutover -DryRun` and `-Prepare -DryRun` (Windows PowerShell 5.1, with a failing fake `az.cmd` first on `PATH` as a guard) — exit 0, every command printed, the fake never called. `-Prepare -DryRun` lists the plan-name read (`appservice plan show … --query [name,sku.name,sku.tier]`) and the apps-on-plan read (`webapp list --query [].[name,appServicePlanId]`).
- `-Prepare` code path (`Invoke-Prepare`, `Get-PlanInfo`, `Assert-FreeTier`, `Get-AppsOnPlan`, `Get-StartupCommand`, `Get-DatabasePath`, `Invoke-AzRead`): `grep -wE "set|delete|update"` = 0 and `config log|log config|Invoke-AzWrite` = 0. A plain substring `set` would match `appsettings list` itself, hence the word grep.
- Existing `old-config.txt` + `-Cutover -DryRun`: SHA-256 identical before/after (`56843698…60fb6e`); also identical across a resumed non-dry `-Cutover` against the fake.
- `grep -nE "appservice plan (update|create)|--sku"` = 0. `grep -nE "appsettings (set|list)|connection-string (set|list|delete)"` = 5 hits (lines 124, 131, 145, 202, 206), each carrying `--query` or `-o none`. `grep -nE "ClientSecret\s*=|ApiToken\s*="` = 0. `<old-resource-group>|<old-reflekt-app>\.azurewebsites` over `deploy` = 0.
- Probe (copy of the script in the scratchpad, against a local fake `az` that returns canned values, no Azure): F1 → guard passes and Prepare prints plan `PlanA`, apps `test-app, other-app` (other-plan app excluded), the missing names, startup and `/home/data/fokus.db`; B1 → exit 1 with the guard message; startup already `dotnet Host.dll` with no snapshot → exit 1, refused; first cutover writes the snapshot; `Database` still listed after delete → exit 1; resumed run with it gone → exit 0, snapshot untouched; neither switch → exit 1.
- 5.1 parser: 0 parse errors.
- Not verified locally: real `az` output shapes (JMESPath results, `appCommandLine`, `sku.tier`), and the calls against the live app — operator Steps 6–7.

## Step 4 — GitHub workflows
**Files:** `.github/workflows/ci.yml` — new; `push` + `pull_request`, workflow-level `permissions: contents: read`; .NET `10.0.x`, Node `24` (npm cache keyed on `client/package-lock.json`); `dotnet build SprintRituals.slnx -c Release`, `dotnet test SprintRituals.slnx -c Release --no-build`, `client` `npm ci` + `npm run build`; starts the Host in the background (`dotnet run --project src/Host -c Release --no-build --launch-profile http`, port 5100), then `pwsh deploy/deploy.ps1 -SmokeOnly -BaseUrl http://localhost:5100 -TimeoutSeconds 120` (must pass) and the same against `http://localhost:5999 -TimeoutSeconds 10` (must fail, inverted in bash); prints `host.log` always.
`.github/workflows/deploy.yml` — new; `workflow_dispatch` only; `concurrency: deploy-production` (no cancel). `build` job (`contents: read`): .NET build + tests as CI, `pwsh deploy/deploy.ps1 -BuildOnly`, upload `artifacts/deploy.zip` (`deploy-zip`, error if missing, 7 days). `deploy` job: `needs: build`, `environment: production`, `id-token: write` + `contents: read`; checkout (for the script only), download the artifact into `artifacts/`, `azure/login` OIDC with `vars.AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID`, then `pwsh deploy/deploy.ps1 -SkipBuild -AppName "$APP_NAME" -ResourceGroup "$RESOURCE_GROUP"`.
**Skills:** None — plan Skill Mapping row 4 `(none)`, TDD no
**Decisions:** Action pins resolved with read-only `git ls-remote` against each action repo (2026-10-30), the commit SHA used (the dereferenced `^{}` commit for the annotated `azure/login` tag): `actions/checkout` v7.0.1 `3d3c42e5…`, `actions/setup-dotnet` v6.0.0 `a98b5685…`, `actions/setup-node` v7.1.0 `949feb24…`, `actions/upload-artifact` v7.0.2 `cf430e03…`, `actions/download-artifact` v8.0.2 `9000827c…`, `azure/login` v3.1.0 `a641126d…`. The two repository variables reach the deploy command through step `env:` rather than inline `${{ }}` in `run:`, so a variable value is never spliced into the shell script. `timeout-minutes` on every job. The CI Host runs in `Development` via the `http` launch profile; `appsettings.json` placeholder Jira values satisfy `JiraOptions`' `ValidateOnStart`, so no secret is needed in CI.
**Deviations:** Plan writes `-AppName ${{ vars.AZURE_WEBAPP_NAME }}` inline; built as `env:` + `"$APP_NAME"` — same values, no script injection path (see Decisions).
**Test problem:** Step run attempt 1 (run `mvuq5tyl-ed3868f1`) — `skipped`, "nothing to run" (no .NET file changed) — not fixed — nothing to fix.
Acceptance (results):
- `deploy.yml` `on:` block = lines 3–4, one key `workflow_dispatch` (YAML parse confirms keys `['workflow_dispatch']`).
- `grep -c "id-token: write" deploy.yml` = 1, at line 51, under `jobs.deploy` (starts line 45); `grep -c "id-token" ci.yml` = 0.
- `grep -nE "npm |dotnet " deploy.yml` → lines 30, 33 only — inside `jobs.build` (`deploy:` starts line 45); the job's `npm` runs inside `deploy.ps1 -BuildOnly`.
- `grep -rn "secrets\." .github/workflows/` = 0; every `uses:` line is `@{40-hex SHA} # v{version}`.
- `grep -rnE "<old-resource-group>|<old-reflekt-app>\.azurewebsites" deploy .github` = 0.
- Both files parse as YAML (PyYAML); per-job permissions as above.
- Not verified locally: any GitHub run (first green run is operator Step 8); `pwsh` behaviour of `deploy.ps1` (first exercised by `ci.yml`'s smoke steps).

## Step 5 — Runbook, ADR-024 amendment and repo pointers
**Files:** `docs/deployment-azure.md` — new operator runbook, modelled on `<fokus-repo>/docs/deployment-azure.md` (read-only): overview table of the pieces, then in plan order Prerequisites, Zero cost (F1 limits, no backup → F10), Demo day (`--sku B1` / `--sku F1` by hand, confirm, Always On off, same-day revert, ~5-person fallback), Growing beyond free, Prepare (Google redirect URI, OIDC app registration with federated credential `repo:ldumit/sprint-rituals:environment:production` and **Website Contributor** on the site only, GitHub `production` environment + five variables, Jira values, `provision.ps1 -Prepare`), Cutover steps 1–9 (access restriction + 403 check, quiet wait, Kudu downloads, checksums, `-Cutover` + `old-config.txt`, secrets, laptop deploy, first sign-in, smoke test, remove restriction; foreign-Admin recovery), Smoke test (Step 8 list), Connecting Jira and inviting the team, Reflekt gate (F9, `<old-resource-group>` untouched), Later deploys, Rollback, Single-instance rule.
`docs/architecture/decisions.md` — ADR-024: the two planned sentences appended after the **Target** F1-limits sentence ("Demo days (owner, 2026-10-30): …") and after the **Data carry-over** Reflekt sentence ("F8 never changes the personal-account Reflekt app …"); nothing else touched (ADR-024 itself sits inside the file's pre-existing uncommitted addition, `git diff --stat` still 13 insertions).
`CLAUDE.md` — under `## Databases`: production paths `/home/data/sprint-rituals/*.db`; new `## Deployment` section pointing at ADR-024, the runbook, `deploy/deploy.ps1` and `deploy.yml`.
**Skills:** None — plan Skill Mapping row 5 `(none)`, docs only
**Decisions:** The federated-credential command writes its JSON to a temp file and passes `@credential.json`, avoiding 5.1/7 native-argument quote differences; the runbook warns that `az … --settings Name=value` leaves the secret in shell history and prefers the portal; it notes the Jira settings must be set before the deploy (otherwise the app starts on `appsettings.json` placeholders and syncs fail); a "Later deploys" section added after the Reflekt gate (workflow + laptop) — not in the plan's list, carries no new decision.
**Deviations:** None
**Test problem:** Step run attempt 1 (run `mvuq940f-21be35a0`) — `skipped`, "nothing to run" (docs only) — not fixed — nothing to fix.
Acceptance (results):
- Runbook `grep -c` per term: `old-config.txt` 2, `access-restriction` 2, `-Prepare` 6, `-Cutover` 4, `/auth/callback` 2, `Website Contributor` 2, `environment:production` 2, `F9` 1, `F10` 1, `Rollback` 1, `F1` 7, `Demo day` 2, `--sku B1` 1, `--sku F1` 1, `Growing beyond free` 2, `<old-resource-group>` 1, `Jira` 7 — all ≥ 1.
- `grep -c "Demo days (owner, 2026-10-30)" docs/architecture/decisions.md` = 1; `grep -c "F8 never changes the personal-account Reflekt app" docs/architecture/decisions.md` = 1.
- Not verified locally: the runbook's live commands (`az ad …`, access restriction, Kudu URLs) — operator Steps 6–8.

## Fix Round 1
**Files:** `.github/workflows/ci.yml` — item 1 (HIGH), fixed: the "Start Host in the background" step gets `env:` `Google__ClientId: ci-placeholder-client-id` and `Google__ClientSecret: ci-placeholder-client-secret` (non-secret placeholders; they bind to `Google:ClientId`/`ClientSecret`, which `AuthConfiguration` binds from the `Google` section and which are empty in `appsettings.json`), so the Google handler's options validation passes and `/` can redirect to `/dashboard/login`. No `secrets.`, no `id-token`.
`deploy/provision.ps1` — item 2 (MEDIUM), fixed: `-Cutover` now reads the target subscription id first (`az account show --query id -o tsv`, with `--subscription` when given — `Get-SubscriptionId`), writes it as `Subscription=` in a new `old-config.txt` beside `App=` and `ResourceGroup=`, and on an existing snapshot (`Read-Snapshot` + `Assert-SnapshotTarget`) compares App, ResourceGroup and Subscription case-insensitively; a missing or different value throws before any write and the file is never written. Under `-DryRun` (no `az` call) the subscription is not compared and the output says so; App and RG still are.
`deploy/deploy.ps1` — item 3 (LOW), fixed: `Invoke-SmokeCheck` computes the remaining time (elapsed rounded up) at the top of each attempt, throws when none is left, passes `--max-time min(20, remaining)` to curl, and accepts a redirect only if the stopwatch is still within `-TimeoutSeconds`; the sleep between attempts is `min(5, remaining)` and skipped when nothing remains.
`CLAUDE.md` — item 4 (LOW), fixed: Databases table adds `reflekt.db` / `ReflektDatabase` / 3rd and `poker.db` / `PokerDatabase` / 4th (order confirmed in `src/Host/Program.cs` `Migrate<>` calls); "Both databases" → "All four databases".
`docs/deployment-azure.md` — Cutover paragraph: the snapshot also records the target's subscription, RG and app, and a run for a different target refuses before changing anything (follows item 2).
**Skills:** None — plan Skill Mapping rows 2–5 `(none)`, TDD no
**Decisions:** Subscription compared as the resolved id from `az account show`, not the `-Subscription` text, so a name and an id for the same subscription match; a snapshot lacking a key (none exists live yet) counts as another target and is refused; dry run skips only the subscription comparison because it runs no `az`.
**Deviations:** None
**Test problem:** Step run attempt 1 (run `mvuqtshb-5fd16f4d`) — `skipped`, "nothing to run" (no .NET file changed) — not fixed — nothing to fix.
**Test problem:** item 3 end to end — `deploy.ps1 -SmokeOnly` against a running Host (`http://localhost:5100`, exit 0) and against an unused port with `-TimeoutSeconds 10` (exit 1, now within 10 s) — not run on this seat (test-entry gate refuses `deploy.ps1`) — owed to the main session; the `pwsh` pair runs in `ci.yml`, first exercised by operator Step 8.
Acceptance (results):
- Windows PowerShell 5.1 parser: 0 parse errors in `deploy/provision.ps1` and `deploy/deploy.ps1`.
- Item 2 probe (copy of `provision.ps1` in the scratchpad, fake `az` function, no Azure): first `-Cutover` writes `App=app-a`, `ResourceGroup=rg-a`, `Subscription=sub-1111`, …; resumed same target → exit 0, snapshot kept; other subscription → exit 1 "belongs to another target (Subscription …)", 0 writes; other app → exit 1, 0 writes; other app with `-DryRun` → exit 1; same target with `-DryRun` → exit 0. SHA-256 of the snapshot identical across all five later runs (`c82981b9…1c19`).
- Item 3 probe (`Invoke-SmokeCheck` extracted by AST, not `deploy.ps1` itself): unused port, timeout 4 → failed after 3.1 s; timeout 0 → failed at once, no probe.
- Item 1: YAML parses; the step's `env` = the two placeholders. `grep -rn "secrets\." .github/workflows/` = 0; `grep -c id-token ci.yml` = 0; every `uses:` still `@{40-hex} # v…`.
- Steps 2–5 Accept greps re-run: `<old-fokus-app>|<resource-group>` in `deploy.ps1` = 0; `appservice plan (update|create)|--sku` = 0; `appsettings (set|list)|connection-string (set|list|delete)` = 5 hits (124, 131, 145, 238, 242), each with `--query` or `-o none`; `ClientSecret\s*=|ApiToken\s*=` = 0; `<old-resource-group>|<old-reflekt-app>\.azurewebsites` over `deploy .github` = 0; runbook term counts unchanged (all ≥ 1, `old-config.txt` 2). `-Prepare` path untouched (`Get-SubscriptionId` is called only from `Save-Snapshot`).

**Main-session check of item 3 (architect, coordinator, 2026-10-30, Windows PowerShell 5.1) — discharges the item-3 test problem above, except the `pwsh` pair:** Host running locally: `-SmokeOnly -BaseUrl http://localhost:5100` → exit 0 (`302 -> /dashboard/login`); `-SmokeOnly -BaseUrl http://localhost:5199 -TimeoutSeconds 10` → exit 1, "did not redirect … within 10 s", 10 s wall time including shell start. — pass

## Go-live (operator Steps 6–8, new app — plan P4)
Run by solo with the owner, 2026-10-30, laptop, Azure CLI 2.88.0, tenant the work tenant, subscription the subscription (`<subscriptionId>`).
- OPERATOR ACTION: pre-checks (read-only) — name `<app-name>` available; runtime `DOTNETCORE|10.0` listed; `<resource-group>` held only `<old-plan-name>` (F1); `<old-fokus-app>` Running. — pass
- OPERATOR ACTION: Step 6 — `az appservice plan create --name <plan-name> … --is-linux --sku F1` — created; `plan show` sku `F1` / `Free` (the create command's own output printed `U13`). No fallback to `<old-plan-name>` needed. — pass
- OPERATOR ACTION: Step 6 — `az webapp create --name <app-name> --plan <plan-name> --runtime "DOTNETCORE:10.0"` — Running, `DOTNETCORE|10.0`. — pass
- OPERATOR ACTION: Step 6 — app registration `<deploy-identity-name>` (appId `<appId>`), service principal, federated credential `github-production` for `repo:ldumit/sprint-rituals:environment:production`; only role: Website Contributor on `…/sites/<app-name>`. — pass (rights were sufficient)
- OPERATOR ACTION: `deploy/deploy-config.ps1` created from the example (git-ignored); clean temporary worktree `<temp>\sprint-rituals-golive` at `72ec0a4`; `deploy.ps1 -BuildOnly` there → `artifacts/deploy.zip` (29 MB). — pass
- OPERATOR ACTION: Step 7 — `az webapp stop --name <old-fokus-app>` → state `Stopped` (backup; not deleted or changed). — pass
- OPERATOR ACTION: Step 7 — access restriction `owner-only` Allow `<owner-ip>/32` priority 100, implicit `Deny all` present; laptop gets 200. — applied; 403 from outside pending the owner's phone check
- OPERATOR ACTION: Step 6 — `provision.ps1 -Prepare` → exit 1 "Could not read the App Service plan": Azure CLI 2.88.0 returns the plan id as `serverFarmId`, the script reads `appServicePlanId` (Carry-Over "read shapes rest on assumed `az` output" confirmed). Every other read shape checked live and correct. — fail, fixed in `d74a84f` (owner approved; field renamed in `Get-PlanInfo` and `Get-AppsOnPlan`, 0 parse errors)
- OPERATOR ACTION: Step 6 — `provision.ps1 -Prepare` re-run at `d74a84f` → plan `<plan-name>` F1 (Free), guard passed; apps on plan: `<app-name>` only; missing required: the six Google/Jira settings; missing optional: the three Xray settings; startup command and `Database` path empty. — pass
- OPERATOR ACTION: Step 7 step 1 — 403 check from outside: the owner's phone on mobile data and an independent fetch from Anthropic's servers both get Azure's placeholder page, before and after an app restart, although the rule reads correctly (Allow `<owner-ip>/32`, `Deny all`, `publicNetworkAccess` Enabled, IPv4 only). Community reports say access restrictions are not enforced on the Free tier (no official Microsoft statement found). — **fail: restriction not enforced on F1; stopped per the plan, nothing deployed**
- OPERATOR ACTION: owner decision, 2026-10-30 — no tier change and no code change for the first sign-in ("I'm going to sign in immediately after you deploy … the whole purpose of this app is to be free!"). Overrides the plan's Must-NOT-Change row "never starts publicly with an empty user table" for this go-live: the exposure is the minutes between the deploy's smoke check and the owner's sign-in, on a new, unpublished address; the foreign-Admin recovery (wipe `/home/data/sprint-rituals/`, restart, sign in again) stays the fallback. The `owner-only` rule stays until Step 7 step 9 but protects nothing on F1. Runbook and plan still describe the restriction as the guard — doc fix owed.
- OPERATOR ACTION: Step 7 step 5 — `provision.ps1 -Cutover` at `d74a84f` → F1 guard passed; snapshot written (empty old values); forwarded headers, no build on deploy, return origin `https://<app-name>.azurewebsites.net`, container logging, HTTP/2, startup `dotnet Host.dll`, HTTPS-only applied; post-condition "no `Database` connection string" held. — pass
- OPERATOR ACTION: Step 7 step 5 secrets — at the owner's request, the six required settings (`Google__ClientId`, `Google__ClientSecret`, `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey`) copied from `<old-fokus-app>` (read only, unchanged) to `<app-name>` by a local script; values never printed; SHA-256 of each copy matches its source. Xray settings not set (optional). — pass
- OPERATOR ACTION: Step 6 Google — owner added origin `https://<app-name>.azurewebsites.net` and redirect URI `…/auth/callback` on the existing OAuth client "Fokus" (the owner's Google Cloud project), keeping every existing entry. — pass
- OPERATOR ACTION: Step 7 step 6 — `deploy.ps1 -SkipBuild` from the clean copy (zip built at `72ec0a4`; app code unchanged at `d74a84f`) → deployed; smoke check passed on attempt 3 (cold start): `302 -> https://<app-name>.azurewebsites.net/dashboard/login`. — pass
- OPERATOR ACTION: 2026-11-03 — owner's browser got Azure's `403 Ip Forbidden` (`x-ms-forbidden-ip: <owner-new-ip>`): the owner's public address had changed from the one in `owner-only`. Owner approved moving the rule: `owner-only` now Allow `<owner-new-ip>/32` priority 100, implicit `Deny all` present; the laptop gets `302 -> /dashboard/login`. — pass. This 403 shows the rule is enforced today, which contradicts the 2026-10-30 "restriction not enforced on F1" line above; the first sign-in, the Smoke test and the rule's removal are still unrecorded.

## Key Decisions
- Step 2: `deploy-config.ps1` variables are `$Deploy*`, dot-sourced in a child scope so they never overwrite passed parameters; `-SkipBuild` checks the zip (and its `Host.dll` entry) before resolving the target; at most one mode switch; `--only-show-errors` on `az webapp deploy`.
- Step 3: `-DryRun` runs no `az` command at all (reads included) and says the SKU guard was not evaluated; every read uses `--query`, every write `-o none`, written on one line per call so the greps read literal text; the snapshot also records app, RG and UTC time.
- Step 4: actions pinned to commit SHAs resolved by read-only `git ls-remote` (azure/login: dereferenced annotated tag); GitHub variables reach `run:` through `env:`; `timeout-minutes` per job; CI Host runs in Development via the `http` launch profile.
- Step 5: federated credential via a JSON file; runbook prefers the portal for secrets; "Later deploys" section.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan Skill Mapping `(none)`, TDD no — environment JSON |
| 2 | None | plan Skill Mapping `(none)`, TDD no — deploy script |
| 3 | None | plan Skill Mapping `(none)`, TDD no — provisioning script |
| 4 | None | plan Skill Mapping `(none)`, TDD no — workflow YAML |
| 5 | None | plan Skill Mapping `(none)`, TDD no — docs only |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Step 2 command checks not run by the developer | medium | reviewer | Nexus test-entry gate refused `deploy/deploy.ps1 -BuildOnly` / `-SkipBuild`; `-SmokeOnly` pair needs a running Host | the main session runs them and records the results under Step 2 (coordinator, 2026-10-30) |
| `provision.ps1` read shapes rest on assumed `az` output | low | reviewer | tested only against a fake `az` (canned `tsv` for `[name,sku.name,sku.tier]`, `[].[name,appServicePlanId]`, `appCommandLine`) | first real check is operator Step 6 `-Prepare` |
| CLAUDE.md `## Databases` table lists only two databases | low | architect | table has `identity.db`, `fokus.db`; the Host runs four (`reflekt.db`, `poker.db` too) | pre-existing; only the production-path note was in scope |

## Unfinished Tests
- Step 2: Test problem: `powershell.exe -File deploy/deploy.ps1 -BuildOnly` and `-SkipBuild` (artifacts deleted) — refused by the Nexus test-entry gate ("runs a build or the test runner itself") — not fixed — tried: running each once; owed to the main session.
- Step 2: Test problem: `-SmokeOnly -BaseUrl http://localhost:5100` against a running Host, and against an unused port with `-TimeoutSeconds 10` — not run — owed to the main session; the `pwsh` pair runs in `ci.yml` on the runner.
- Fix Round 1: Test problem: item 3 end to end — `deploy.ps1 -SmokeOnly` against a running Host and against an unused port with `-TimeoutSeconds 10` — not run on this seat (gate refuses `deploy.ps1`) — owed to the main session.

## Deviations from Plan
- Step 4: the deploy command takes the app name and resource group through step `env:` (`"$APP_NAME"`, `"$RESOURCE_GROUP"`) instead of inline `${{ vars.… }}` in `run:` — same values, no script-injection path.

*Status: COMPLETE — developer, 2026-10-30*
