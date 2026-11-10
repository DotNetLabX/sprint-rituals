# F8 — Azure Deployment

**Feature Spec:** None — technical feature, ADR-collapsed definition: **ADR-024** (`docs/architecture/decisions.md` § Hosting & deployment ADRs) + backlog row F8 + ratified proposal `docs/proposals/azure-deployment-handoff.md`.
**Model:** claude-opus-5-5
**Rule list:** not applicable — deployment wiring commits no rule-shaped domain behaviour (no boundaries, invariants or computed outcomes).
**Size:** 5 developer-built steps · 8 files · fullChecks: none — no fullChecks list declared
**Lane:** light — owner's level
**Build lane:** fastest — lighter checks — recommended: fastest — lighter checks
**Skills per step:** on

## Context

SprintRituals (one Host: Identity + Fokus + Reflekt + Poker) has no deploy path. ADR-024 fixes the shape: replace standalone Fokus on the work-tenant Linux App Service `<old-fokus-app>` (RG `<resource-group>`) on the **free F1 plan** (zero cost is binding), one instance, SQLite on `/home` exactly as the live apps run it, one deploy script run by a manual GitHub workflow with OIDC sign-in. Everything is config-driven so the same scripts can target a personal free subscription later.

**Target changed (owner, 2026-10-30, plan P4):** the suite goes to a **new** app `<app-name>` on its own F1 plan `<plan-name>` in `<resource-group>` (Poland Central), beside Fokus; `<old-fokus-app>` is stopped at go-live (`az webapp stop`, never deleted or changed) and kept as the backup until the new app is proven. Where older text below says "replace `<old-fokus-app>`", "old app" or "cutover", read it for the new app: no old app is backed up, quiesced or rolled back; rollback is stopping or deleting the new app and starting `<old-fokus-app>` again (`az webapp start`).

**Owner goal (2026-10-30):** the whole suite deployed together, free, for a demo — the three products sharing one sign-in (Identity), connected to the team's Jira, and shown working *together* (Reflekt's sprint picker and Poker's tickets come from Fokus over gRPC), not as separate apps. Free now, extendable later (→ Step 5 § Growing beyond free). The old Reflekt app (`<old-reflekt-app>.azurewebsites.net`, RG `<old-resource-group>`) is on the owner's personal account and is not touched by F8 (its data comes over in F9).

Prior artifacts (grep `deploy|azure` over `docs/proposals`, `docs/specs`, `docs/backlog.md`): historical Fokus/Reflekt deployment references under `docs/specs/{fokus,reflekt}/`, and the proposal above (now Ratified → this feature). No other plan implements this deployment.

Facts verified at plan time (2026-10-05, re-checked 2026-10-30):
- `src/Host/appsettings.json` pins `Kestrel:Endpoints:Http:Url = http://localhost:5100` and `Grpc` = `http://localhost:5150` (Http2); all five `Grpc:Services:*:Url` = `http://localhost:5150`. Explicit endpoints mean App Service's port variable is ignored → the public endpoint must be re-pinned for Production.
- `Auth:AllowedReturnOrigins` exists **only** in `appsettings.Development.json` (read by `Identity.API/Features/Auth/Login/LoginEndpoint.cs:26`); the SPAs send an absolute `returnUrl`, so Production needs it.
- Google `CallbackPath = "/auth/callback"` (`Identity.API/Auth/AuthConfiguration.cs:45`). No `UseForwardedHeaders` in `src`. Invitation links are built from `Request.Scheme` (`Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs:39`).
- The first Google user with no users in the DB becomes Admin (`AuthConfiguration.cs:80-84`, `:194`), with no domain restriction.
- `Program.cs` runs `app.Migrate<T>()` for Identity → Fokus → Reflekt → Poker before `app.Run()`; `/` redirects unauthenticated users to `/dashboard/login`.
- `src/Host/wwwroot/` is git-ignored and produced by `client` `npm run build`; `client/package-lock.json` is tracked. Host assembly = `Host.dll`. Baseline `dotnet test SprintRituals.slnx -c Release`: 339 passed (second reader, round 1).
- PowerShell 7 is **not** installed on the owner's machine (Windows PowerShell 5.1 only); the GitHub Ubuntu runner has PowerShell 7.
- No `.github/` folder exists. Remote: `github.com/ldumit/sprint-rituals`.
- Real-time: three SignalR hubs (`/hubs/sprint` Fokus dashboard, `/hubs/retro`, `/hubs/poker`), each browser page opens one connection. All three clients use SignalR's default transports (`client/apps/dashboard/src/stores/dailyProgressStore.ts:33`, `client/apps/retro/src/composables/useRetroHub.ts:32`, `client/apps/poker/src/composables/usePlanningHub.ts:25` — no `transport`/`skipNegotiation`), so past F1's 5 WebSocket connections a browser falls back to server-sent events or long polling instead of failing.
- `git diff --stat 4affe6a -- src/Host/appsettings.json src/Host/appsettings.Development.json` is empty at HEAD `bdb682f` (2026-10-30).

## Scope

**In scope:** Production config file; `deploy/deploy.ps1` + `deploy/provision.ps1`; GitHub CI + manual deploy workflows; runbook (including the demo-day procedure and the growth path); ADR-024 amendment; operator preparation, cutover and smoke test.

**Out of scope:** old Reflekt data carry-over (**F9**); nightly DB snapshot (**F10**, owner-deferred); old Fokus data migration (ADR-024: not migrated); any **standing** paid tier (the one exception is the operator's same-day demo B1, runbook only), managed database, containers, custom domain, deployment slots, scale-out; **any change to the personal-account Reflekt app** (`<old-reflekt-app>.azurewebsites.net`, RG `<old-resource-group>`) — F9 reads it later, nothing here writes to it.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | `src/Host/appsettings.Production.json` keys | gap: no hosting/environment-config skill |
| 2 | (none) | — | no | `deploy/deploy.ps1` contract | gap: no deploy-script skill |
| 3 | (none) | — | no | `deploy/provision.ps1` contract | gap: no Azure provisioning skill |
| 4 | (none) | — | no | `.github/workflows/ci.yml`, `deploy.yml` | gap: no CI/CD workflow skill |
| 5 | (none) | — | no | `docs/deployment-azure.md`, `docs/architecture/decisions.md` ADR-024, `CLAUDE.md` | — (docs only, expected) |
| 6–8 | (operator) | — | no | live Azure / Google / GitHub / Jira actions | — |

Skill verification for the `None` rows (re-run 2026-10-30 against the full skill list, with skills-per-step on): the actions (write environment JSON, author PowerShell/az scripts, author GitHub workflow YAML, amend an ADR) match no skill description — `service-infra-conventions` covers options binding/pipeline/validators inside a service, `create-host` composes services into one host (already done), `persistence-patterns` covers EF Core persistence, `service-registration` covers DI. No step adds testable runtime behaviour, so every step is `TDD: no`.

## Domain Model Changes

None.

## Data Model Changes

None — no migrations, no change to how databases are created or opened.

## Implementation Steps

### Step 1 — Production configuration file
Effort points: 1
Create `src/Host/appsettings.Production.json` (loaded only when `ASPNETCORE_ENVIRONMENT=Production`, App Service's default). Non-secret, target-independent values only:
- `Kestrel:Endpoints:Http:Url = http://+:8080` (keep `Protocols: Http1AndHttp2`). The `Grpc` endpoint and `Grpc:Services:*` stay inherited from `appsettings.json` (in-container loopback).
- `ConnectionStrings`: `IdentityDatabase`, `Database`, `ReflektDatabase`, `PokerDatabase` → `Data Source=/home/data/sprint-rituals/{identity|fokus|reflekt|poker}.db`.
- No return origins here — they depend on the target app's URL and are set by Step 3.

Accept (PowerShell, `ConvertFrom-Json` on the file):
- `.Kestrel.Endpoints.Http.Url` = `http://+:8080`; `.Kestrel.Endpoints.Grpc` is absent.
- `.ConnectionStrings` has exactly the four keys above with exactly those four values.
- Top-level keys are exactly `Kestrel` and `ConnectionStrings` (so no `Google`, `Jira`, `Xray` or secret key).
- `git diff --stat 4affe6a -- src/Host/appsettings.json src/Host/appsettings.Development.json` is empty (pre-F8 baseline commit; local dev untouched). A non-F8 commit touching either file is named in `implementation.md`.
- `dotnet publish src/Host -c Release -o artifacts/publish` output contains `appsettings.Production.json`.

### Step 2 — Deploy script
Effort points: 5
Create `deploy/deploy.ps1`, `deploy/deploy-config.example.ps1`, and add `deploy/deploy-config.ps1`, `deploy/.cutover/` and `artifacts/` to `.gitignore`. The script runs unchanged under **Windows PowerShell 5.1 and PowerShell 7** (laptop has 5.1; the runner has 7). Pattern reference (feature surface, not a skill pattern): `<fokus-repo>/deploy.ps1` — read-only, never modified.
- Inputs: `-AppName`, `-ResourceGroup`, optional `-Subscription`; when omitted, read from `deploy/deploy-config.ps1` if present; fail with a clear message if still missing.
- Modes: default = build then deploy; `-BuildOnly` = build and zip, no Azure call; `-SkipBuild` = deploy an existing `artifacts/deploy.zip`, failing fast with a clear message if the zip is missing or has no `Host.dll` entry (checked inside the zip — the deploy job has no unpacked publish folder); `-SmokeOnly -BaseUrl {url}` = run only the smoke check.
- Build: `client` → `npm ci` + `npm run build`; `dotnet publish src/Host -c Release -o artifacts/publish`; zip to `artifacts/deploy.zip` with **forward-slash entry paths** (via .NET `System.IO.Compression`, not `tar -a` or 5.1's `Compress-Archive`).
- Deploy: `az webapp deploy --type zip --clean true --track-status false`, output suppressed; then the smoke check.
- Smoke check: polls `{BaseUrl}/` (default `https://{AppName}.azurewebsites.net`) **without following redirects**, until it answers a 3xx whose location ends in `/dashboard/login`; transient errors retry; `-TimeoutSeconds` (default 300) then exit non-zero. The probe is non-interactive on both shells — `curl.exe` on Windows (never the 5.1 `curl` alias), `curl` elsewhere.
- Stop on the first failure: `$ErrorActionPreference = 'Stop'` plus an exit-code check after every native command, exit non-zero.

Accept:
- `grep -n "<old-fokus-app>\|<resource-group>" deploy/deploy.ps1` = 0 hits (values live in the example config only).
- `powershell.exe -File deploy/deploy.ps1 -BuildOnly` (Windows PowerShell 5.1) exits 0 and produces `artifacts/deploy.zip`. Listing its entries shows `wwwroot/dashboard/index.html`, `wwwroot/retro/index.html`, `wwwroot/poker/index.html`, `Host.dll`, `appsettings.Production.json`, and no entry contains `\`.
- `powershell.exe -File deploy/deploy.ps1 -SkipBuild` with `artifacts/` deleted exits non-zero with the missing-artifact message, before any `az` call.
- With the Host running locally (`dotnet run --project src/Host`), `powershell.exe -File deploy/deploy.ps1 -SmokeOnly -BaseUrl http://localhost:5100` exits 0; against an unused port with `-TimeoutSeconds 10` it exits non-zero. The same two runs pass under `pwsh` on the CI runner (a `ci.yml` step).
- Every native-command call in the script is followed by an exit-code check (read and confirm).

### Step 3 — Provisioning script (two phases)
Effort points: 5
Create `deploy/provision.ps1` — idempotent `az` calls, same config as Step 2, same 5.1/7 compatibility, and `-DryRun` (prints every `az` command, changes nothing).
- **Zero-cost guard (both phases):** read the site's plan SKU; if it is not `F1`/`Free`, stop with an error. The script never changes a plan or SKU (a demo-day B1 is the operator's, by hand — Step 5).
- **`-Prepare`** — **read-only** (any settings change restarts an App Service app, so nothing is changed here): the SKU guard; print the App Service plan's **name** and the **names** of every app on that plan (scaling the plan for a demo day scales them all); report, **by name only**, the missing required secrets — `Google__ClientId`, `Google__ClientSecret`, `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey` — and missing optional ones (`Xray__ClientId`, `Xray__ClientSecret`, `Xray__BaseUrl`); print the current startup command and the current `Database` connection-string **path** (so Step 7 knows what to back up).
- **`-Cutover`** (run only inside Step 7):
  - **Snapshot, write-once:** if `deploy/.cutover/old-config.txt` does not exist, record the current startup command and `Database` path there, refusing if the startup command is already `dotnet Host.dll`. If it exists, keep it untouched (a resumed cutover).
  - Then set: app settings `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `SCM_DO_BUILD_DURING_DEPLOYMENT=false`, `Auth__AllowedReturnOrigins__0=https://{AppName}.azurewebsites.net`; container logging to filesystem (`az webapp log config --docker-container-logging filesystem`); HTTP/2 on; HTTPS-only on; startup command `dotnet Host.dll`; delete the `Database` connection string.
  - Post-condition: `connection-string list` returns no `Database` name, else exit non-zero. The printed note says the old database file itself is not touched.
  - Every step is idempotent, so a re-run after a partial failure completes the cutover.
- **Secret-safe output:** every `az` call that can return settings or connection strings uses `-o none` (writes) or a `--query` that returns names or the single path only (reads).

Accept:
- `powershell.exe -File deploy/provision.ps1 -Cutover -DryRun` prints its command list and runs no `az` command that changes state; `-Prepare` contains no state-changing `az` call at all (`grep` its code path for `set|delete|update|config log` = 0 hits).
- `-Prepare -DryRun` output includes the plan-name read and the apps-on-plan read (names only, via `--query`).
- `-Cutover` against an existing `deploy/.cutover/old-config.txt` leaves the file byte-identical (checksum before and after, in `-DryRun`).
- `grep -nE "appservice plan (update|create)|--sku" deploy/provision.ps1` = 0 hits.
- `grep -nE "appsettings (set|list)|connection-string (set|list|delete)" deploy/provision.ps1`: every hit line also carries `-o none` or `--query`.
- `grep -nE "ClientSecret\s*=|ApiToken\s*=" deploy/provision.ps1` = 0 hits.

### Step 4 — GitHub workflows
Effort points: 3
Create `.github/workflows/ci.yml` and `.github/workflows/deploy.yml`. Pin every third-party action to a full commit SHA, with the version in a comment.
- `ci.yml`: on `push` and `pull_request`; `permissions: contents: read`; .NET 10 SDK, Node 24; `dotnet build SprintRituals.slnx -c Release`, `dotnet test SprintRituals.slnx -c Release --no-build`, `client` `npm ci` + `npm run build`; then the two `-SmokeOnly` runs from Step 2 under `pwsh`, against the Host started in the background.
- `deploy.yml`: triggered **only** by `workflow_dispatch`; `concurrency` group so two deploys never overlap. Two jobs:
  - `build` — `permissions: contents: read`; .NET tests as in CI, then `pwsh deploy/deploy.ps1 -BuildOnly`, then upload `artifacts/deploy.zip`.
  - `deploy` — `needs: build`; `environment: production`; `permissions: id-token: write, contents: read`; download the artifact; `azure/login` with OIDC using repository **variables** `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`; then `pwsh deploy/deploy.ps1 -SkipBuild -AppName ${{ vars.AZURE_WEBAPP_NAME }} -ResourceGroup ${{ vars.AZURE_RESOURCE_GROUP }}`. No `npm` or `dotnet` command runs in this job.

Accept:
- `deploy.yml`'s `on:` block has exactly one key, `workflow_dispatch` (the block is under 3 lines; read and confirm).
- `grep -c "id-token: write" .github/workflows/deploy.yml` = 1, and it sits under `jobs.deploy`. `grep -c "id-token" .github/workflows/ci.yml` = 0.
- `grep -nE "npm |dotnet " .github/workflows/deploy.yml` hits only lines inside `jobs.build`.
- `grep -rn "secrets\." .github/workflows/` = 0 hits; every `uses:` line ends in a 40-character SHA.
- `grep -rnE "<old-resource-group>|<old-reflekt-app>\.azurewebsites" deploy .github` = 0 hits (the personal-account Reflekt app is never a target).
- What a PASS here does **not** prove: the first green GitHub run is Step 8's (operator).

### Step 5 — Runbook, ADR-024 amendment and repo pointers
Effort points: 3
Create `docs/deployment-azure.md` (operator runbook, following the structure of `<fokus-repo>/docs/deployment-azure.md`). It covers, in order:
- **Prerequisites:** Azure CLI; Node 24 and the .NET 10 SDK for laptop deploys; PowerShell 7 is optional.
- **Zero cost:** the app stays on F1. Its limits: one allowance of 60 CPU-minutes a day — when it is used up the site stops until the next day's reset; 5 WebSocket connections for all three products together — past that, browsers fall back to slower transports and keep working; the site sleeps after about 20 idle minutes, so the first request is slow. No built-in backup; the nightly snapshot is F10.
- **Demo day** — for a demo with more than about 5 people live, or one longer than a short walkthrough:
  - About 30 minutes before, from the laptop under your own `az login` (the GitHub deploy identity has no rights on the plan, on purpose): `az appservice plan update --name {plan} --resource-group <resource-group> --sku B1`, using the plan name `-Prepare` printed. Every app on that plan moves with it, and each restarts once.
  - Confirm with `az appservice plan show … --query sku.name` = `B1`; open each of the three apps once to warm it up.
  - Leave Always On off — Free cannot run it, so it would block the scale-back.
  - The same day, after the demo: `az appservice plan update … --sku F1`, then confirm `F1`. B1 costs roughly $0.02 an hour against the Sponsorship credit (list price, unverified); a forgotten scale-back costs about $13 a month.
  - Without the rights or the credit to scale: keep the live audience to about 5 people, and let the others watch a shared screen.
- **Growing beyond free** — what each later need changes, none of it built now: no CPU quota, Always On, backups → a standing B1 (same commands as Demo day; drop the `F1` guard in `provision.ps1` by a new ADR); more than one instance → RabbitMQ transport, a SignalR backplane and a managed database first (ADR-024 Single instance); lock errors on `/home` → a managed database (ADR-024 Storage); a nicer address → custom domain. The scripts, workflows and config files stay as they are for all of these.
- **Prepare** (Step 6).
- **Cutover**, as one sitting with a downtime window (Step 7), including the access restriction during first sign-in.
- **Smoke test** (Step 8).
- **Connecting Jira and inviting the team** — the Jira values are App Service settings set in Step 7; the first sprint sync pulls the developers; invitations come from Fokus.
- **Reflekt gate:** existing Reflekt users stay on the old personal-account app (`<old-reflekt-app>.azurewebsites.net`, RG `<old-resource-group>`) until F9 has migrated their data. F8 never changes that app.
- **Rollback:** stop the site; redeploy the saved old `wwwroot` zip; restore the startup command and the `Database` path from `deploy/.cutover/old-config.txt`; start the site.
- **Single-instance rule.**

Amend **ADR-024** in `docs/architecture/decisions.md` (the file may carry another session's uncommitted edits — change only these two sentences):
- **Target**, before: `Known F1 limits, accepted by the owner: one shared allowance of 60 CPU-minutes/day and 5 WebSocket connections for all three products; SignalR falls back to slower transports past the fifth connection.`
  After: the same sentence, then `Demo days (owner, 2026-10-30): for a demo with more people than F1 carries, the operator scales the plan to B1 by hand for that day and back to F1 the same day (runbook § Demo day); no script or workflow ever changes the tier.`
- **Data carry-over**, before: `Old **Reflekt** data (personal account) **is** migrated, by a separate feature (\`F9\`), before Reflekt users switch.`
  After: the same sentence, then `F8 never changes the personal-account Reflekt app (\`<old-reflekt-app>.azurewebsites.net\`, RG \`<old-resource-group>\`).`

Add a short **Deployment** section to `CLAUDE.md` pointing at ADR-024 and the runbook. Add a note under the `Databases` table that production paths are `/home/data/sprint-rituals/*.db`.

Accept:
- `grep -c` ≥ 1 in the runbook for each of `old-config.txt`, `access-restriction`, `-Prepare`, `-Cutover`, `/auth/callback`, `Website Contributor`, `environment:production`, `F9`, `F10`, `Rollback`, `F1`, `Demo day`, `--sku B1`, `--sku F1`, `Growing beyond free`, `<old-resource-group>`, `Jira`.
- `grep -c "Demo days (owner, 2026-10-30)" docs/architecture/decisions.md` = 1 and `grep -c "F8 never changes the personal-account Reflekt app" docs/architecture/decisions.md` = 1.

### Step 6 — Prepare, with no change to the live app (Owner: operator)
- Create the free plan and the app (owner, 2026-10-30 — plan P4): `az appservice plan create --name <plan-name> --resource-group <resource-group> --location polandcentral --is-linux --sku F1`, then `az webapp create --name <app-name> --resource-group <resource-group> --plan <plan-name> --runtime "DOTNETCORE:10.0"` (check the runtime string with `az webapp list-runtimes --os linux`). If the plan create is refused for a second free Linux plan, use `--plan <old-plan-name>`. HTTPS-only stays a `-Cutover` setting.
- Add the Google redirect URI `https://<app-name>.azurewebsites.net/auth/callback` on the existing OAuth client, keeping every existing URI.
- Create the OIDC app registration in the work tenant:
  - a federated credential for `repo:ldumit/sprint-rituals:environment:production`;
  - the role **Website Contributor**, scoped to the `<app-name>` site only.
- In GitHub:
  - create the `production` environment, with deployment branches limited to `main`;
  - add the five repository variables.
- Have the Jira values ready (instance URL, a service account's email and API token, project key).
- Run `provision.ps1 -Prepare` (read-only). Note the old database path, the plan name and the other apps on the plan it prints.
- Any secret it reports missing is set inside the Step 7 window, because a settings change restarts the app.
- Confirm you have the rights to create the app registration. If you don't, the laptop deploy path still works, and this is recorded in `implementation.md`.

### Step 7 — Cutover, in one sitting, from the laptop (Owner: operator)
The downtime window runs from step 1 to step 9. The cutover deploy runs from the laptop, because the GitHub runner is outside the allowed IP. Later deploys use the workflow.
1. Add an access restriction allowing only your own IP (`az webapp config access-restriction add`). Confirm from another network (for example a phone on mobile data) that the site answers 403. **If the restriction cannot be applied or confirmed, stop here** — never start the new app publicly with an empty user table.
2. — (new app: no old app to quiesce.)
3. — (new app: no old build or database to back up; `<old-fokus-app>` is never touched.)
4. —
5. Run `provision.ps1 -Cutover`, then set any secret `-Prepare` reported missing — Google and Jira included (portal, or `az … -o none`).
6. Run `deploy.ps1` from the laptop. Its smoke check runs from your allowed IP.
7. Sign in with Google first, then confirm that the Admin user and the company domain are yours.
8. Run the Step 8 smoke test while still restricted.
9. Remove the access restriction.

If a foreign Admin or company domain appears, keep the restriction, delete every file in `/home/data/sprint-rituals/` (all four databases, because the company domain lives in Fokus's), restart the site, and sign in again. Nothing worth keeping exists there yet.

### Step 8 — Smoke test (Owner: operator)
Record each result in `implementation.md` as an `OPERATOR ACTION` line:
- `/` redirects to `/dashboard/login`.
- The Google sign-in URL's `redirect_uri` starts with `https://`, and the auth cookie is marked Secure.
- On the SSH console (Kudu), `ls -la /home/data/sprint-rituals/` shows `identity.db`, `fokus.db`, `reflekt.db` and `poker.db`. `<old-fokus-app>` is stopped, not deleted (`az webapp show … --query state` = `Stopped`).
- Run a Fokus sprint sync against the team's Jira. Developers appear, and the log has no Person-upsert warning.
- Create a retro. The sprint picker lists the synced sprints (sprint catalog over gRPC), and its sprint-context panel loads (sprint analytics over gRPC).
- Create a Poker sprint session. Its tickets load (Poker to Fokus over gRPC).
- Open a Poker room as a guest in a second browser. Votes appear live, and the browser network tab shows a WebSocket upgrade (101).
- Create an invitation. The link starts with `https://<app-name>.azurewebsites.net/invite/`, and a second Google account can redeem it.
- Restart the site. Data is still there.

A failure here starts a fix round. It is not a done-check Fail of Steps 1–5.

## Cross-Service Changes

None. gRPC contracts unchanged; the loopback endpoint keeps `http://localhost:5150` inside the container.

## Migration Notes

No EF migrations. On first boot the four databases are created fresh under `/home/data/sprint-rituals/` by the existing `Migrate<T>()` + `EnsureDataDirectory`, in EF Core's default mode (as the live apps run today).

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Local dev keeps `localhost:5100`, dev return origins, dev DB paths | `git diff --stat 4affe6a` of `appsettings.json` + `appsettings.Development.json` empty (Step 1) | pinned |
| The new app never starts publicly with an empty user table | Step 7 step 1 (confirmed 403 from outside, else stop) | accepted-gap (operator procedure — only provable live) |
| Zero cost: no script or workflow ever changes the tier; the plan is F1 except an operator demo day, reverted the same day | Step 3 SKU guard + `--sku`/`plan update` grep = 0; runbook § Demo day | pinned (scripts) · accepted-gap (the same-day revert is operator procedure) |
| The personal-account Reflekt app is never touched | Step 4 `<old-resource-group>|<old-reflekt-app>.azurewebsites` grep = 0 over `deploy` and `.github` | pinned |
| Old `/home/data/fokus.db` is never opened by the new app | Step 1 exact connection-string values + Step 3 `-Cutover` post-condition (no `Database` connection string, non-zero exit otherwise) | pinned |
| Nothing auto-deploys | Step 4 `on:` block check | pinned |
| The deploy identity is never present while `npm`/`dotnet` run | Step 4 `id-token` + `npm`/`dotnet` location greps | pinned |
| No secret value in git or in script output | Step 3 output-suppression grep; Step 4 `secrets.` grep; `deploy/deploy-config.ps1` git-ignored | pinned |
| gRPC self-calls keep working | Step 8 retro picker, sprint-context panel, Poker sprint session | accepted-gap (only provable live — operator) |

## Testing Strategy

- No new unit tests: no step changes runtime behaviour.
- `dotnet build` + `dotnet test SprintRituals.slnx` green; `client` `npm run build` green.
- Step 2 local `-BuildOnly` run and zip listing; Step 3 `-DryRun` runs.
- Live behaviour (https scheme, gRPC loopback, SignalR on F1, SQLite on `/home`, Jira sync) is the operator's Step 8 — a developer PASS does **not** prove these.

## KB Impact

None — no business rules, formulas or data flows change.

## Decisions

| Decision | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|
| Production DB paths live in `appsettings.Production.json`, and `-Cutover` deletes the App Service `Database` connection string | Config in git, and removes the override that would point the new app at the old file | Set the four strings as App Service connection strings (portal-only, invisible in review) | high — precedence verified (round 1) | decided |
| Return origin is set by `provision.ps1` from the app name, not in the JSON | Keeps the JSON target-independent, so a personal free subscription needs no code change | Hard-code the `<old-fokus-app>` URL in `appsettings.Production.json` | high — one setting, no code | decided |
| Scripts target Windows PowerShell 5.1 and PowerShell 7 | The owner's laptop has only 5.1; the runner has 7; no install needed | Require PowerShell 7 on the laptop | high — verified `where.exe pwsh` none | decided |
| `provision.ps1` split into a read-only `-Prepare` and a write-once-snapshot `-Cutover` | Any settings change restarts the app, so every change happens inside the restricted cutover window; a retry never overwrites the rollback snapshot | One provisioning run before deploy (would break the old app early) | high — closes R2-02, R2-08 | decided |
| Cutover quiesces the old app with the access restriction, not by stopping the site | Stopping the site may also stop the deployment endpoint; with users blocked, the idle old app plus its `-wal`/`-shm` files gives a consistent copy | Stop the site, then deploy | medium — the idle-copy relies on 5 quiet minutes | decided |
| The cutover deploy runs from the laptop | The GitHub runner's IP is outside the owner-only restriction | Temporarily allow GitHub's IP ranges | high | decided |
| Deploy workflow builds in one job and deploys from its artifact in a second job | The deploy identity is never available while third-party build code runs | One job building and deploying | high | decided |
| OIDC role scoped to the one site (Website Contributor); `production` limited to `main` | Least privilege for an unattended identity; it also cannot scale the plan, so a demo-day tier change is always a person's act | Contributor on the resource group | high | decided |
| Demo day scales the plan to B1 by hand, written as runbook commands, not a script switch | Least code; the guard keeps every script on F1; see plan P1 | A `provision.ps1 -DemoTier` switch (more code, and it would need plan rights); staying on F1 and relying on the transport fallback (the CPU allowance can stop the site mid-demo) | medium — the price is unverified and Sponsorship credit is presumed available | decided |
| `-Prepare` prints the plan name and the other apps on it | Scaling the plan scales every app on it, so the operator must see them before a demo day | Leave it to the portal | high — read-only, names only | decided |

## Decisions and why

### P1 — A demo with more people runs on a same-day B1
- **By:** architect (owner asked the architect to decide, 2026-10-30)
- **Decided:** standing tier F1; for a demo with more than about 5 live people the operator scales the plan to B1 by hand that day and back to F1 the same day; without rights or credit, the live audience is kept to about 5.
- **Why:** the owner: "Free tier allows only 5 WebSocket connections and 60 CPU-minutes a day: decide how a demo with more people is handled (e.g. temporary Basic tier on demo day)" and "I need a free solution for now in Azure, but which can be extendable later on." Architect's reasoning: past 5 connections the apps keep working on slower transports (verified in the three hub clients), so the connection cap is not the risk — the 60 CPU-minute allowance is, because when it runs out the site stops until the next day. A same-day B1 removes both limits for cents of Sponsorship credit, and F1 stays the standing tier.
- **Turned down:** stay on F1 and rely on the fallback — zero cost, but the site can stop mid-demo; a script switch — more code for a few-times-a-year act; a standing B1 — breaks the binding zero cost.
- **Rests on:** F1 limits as the owner stated them; B1 costing cents an hour on the Sponsorship credit (unverified list price); the operator having rights to scale the plan.
- **Confidence:** medium — the price and the operator's plan rights are unverified; both are checked on the first demo day.
- **Source:** owner message, 2026-10-30
- **State:** current

### P2 — The personal-account Reflekt app stays untouched
- **By:** owner
- **Decided:** F8 deploys and configures only `<old-fokus-app>`; `<old-reflekt-app>.azurewebsites.net` (RG `<old-resource-group>`) is not changed; its data comes over in F9.
- **Why:** the owner: "The old Reflekt app (<old-reflekt-app>.azurewebsites.net, RG <old-resource-group>) is on the owner's personal account; leave it untouched (F9 later)."
- **Turned down:** none offered
- **Rests on:** the old Reflekt app staying on the owner's personal account
- **Confidence:** high — owner's words
- **Source:** owner message, 2026-10-30
- **State:** current

### P3 — The whole suite deploys together, free, extendable later
- **By:** owner
- **Decided:** one Host with Identity, Fokus, Reflekt and Poker on `<old-fokus-app>` (F1), connected to the team's Jira; the growth path is documented, not built.
- **Why:** the owner: "I would like to have the whole thing that we built here deployed, so then I can demo it, connect it to a Jira … those three applications with identity, and then I can demo them working together … Not as independent. And I need a free solution for now in Azure, but which can be extendable later on."
- **Turned down:** none offered — the one-Host shape is ADR-001 and ADR-024
- **Rests on:** ADR-024 (F1, single instance, SQLite on `/home`)
- **Confidence:** high — owner's words
- **Source:** owner message, 2026-10-30
- **State:** replaced by plan P4 (target app only; the rest stands)

### P4 — A new app beside Fokus instead of replacing it
- **By:** owner
- **Decided:** deploy to a new app `<app-name>` on its own free plan `<plan-name>` (fallback: `<old-plan-name>` if a second free Linux plan is refused), RG `<resource-group>`, Poland Central; `<old-fokus-app>` is stopped at go-live — not deleted, settings and files kept — as the backup until the owner sees the new app running; `az webapp start` brings it back.
- **Why:** the owner: "the only problme is the name so I would create a new app service same free concept as fokus called: https://<app-name>.azurewebsites.net". Architect's reading: an own plan keeps old Fokus and the suite from sharing one free plan's memory and CPU, and makes a demo-day B1 affect only the suite.
- **Turned down:** replacing `<old-fokus-app>` in place (the 2026-10-05 decision); putting the app on `<old-plan-name>` (kept as the fallback).
- **Rests on:** Azure allowing a second free Linux plan in this subscription and region (unverified — the fallback covers it).
- **Confidence:** high for the new app (owner's words); medium for its own plan — the free-plan limit is unverified.
- **Source:** owner messages, 2026-10-30 ("we can pause <old-plan-name> for a while, not delete because i need it as backup untill I see this new one running")
- **State:** current

## Plan Review

Round 1 (Codex primary NO-GO, Sonnet second reader REVISE; 13 merged findings) — merge and fold in `review-critic.md` § Round 1. The owner ruled the storage premise (R1-01) and deferred backups (R1-08) on 2026-10-05. Every other finding is folded into Steps 1–8 above.
Round 2 (Codex, delta read; NO-GO, 1 HIGH + 8 MEDIUM) — all 9 folded (`review-critic.md` § Round 2). No CRITICAL was reported, so the schedule ends after round 2: the NO-GO is the verdict on the pre-fold plan, not on this one.
Revision 2026-10-30 (owner's new facts; self-review, disclosed — no critic round): each round-2 fold re-checked against this plan's text (R2-01 Step 2 `-SkipBuild`; R2-02 Step 3 `-Prepare` read-only; R2-03 Step 8 Kudu `ls`; R2-04 Step 1 baseline `4affe6a`, re-run empty today; R2-05 Step 7 all four databases; R2-06 Step 7 step 1; R2-07 Steps 2 and 7; R2-08 Step 3 write-once; R2-09 Steps 2 and 4) — all present. Added: demo-day procedure and growth path (Step 5), ADR-024 amendment (Step 5), plan-and-apps read (Step 3), Reflekt hands-off grep (Step 4), Jira readiness (Steps 6–8), Must-NOT-Change rows, P1–P3. Reconciled: Scope ↔ Step 5 runbook ↔ ADR-024 amendment ↔ Must-NOT-Change zero-cost and Reflekt rows; Step 3 `-Prepare` ↔ Step 6 ↔ runbook § Demo day; Decisions ↔ P1.

## Open Questions

None.
