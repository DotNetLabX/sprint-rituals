# SprintRituals — Azure Deployment

One Linux App Service runs the whole suite: the single Host (Identity + Fokus + Reflekt + Poker) and the three Vue SPAs it serves from `wwwroot/`. No containers — the built-in .NET 10 runtime runs `dotnet Host.dll`. SQLite lives on the persistent `/home` share at `/home/data/sprint-rituals/{identity,fokus,reflekt,poker}.db`.

The decision behind this shape is **ADR-024** (`docs/architecture/decisions.md`). The target is a **new** work-tenant app, `<app-name>` (`https://<app-name>.azurewebsites.net`), on its own free plan `<plan-name>`, in resource group `<resource-group>` (Poland Central), beside the standalone Fokus app `<old-fokus-app>`. At go-live `<old-fokus-app>` is **stopped, never deleted or changed** — it is the backup until the new app is proven, and `az webapp start` brings it back (§ Rollback). Every value is config-driven (`deploy/deploy-config.ps1`, the GitHub variables), so the same scripts can target a personal free subscription later.

| Piece | What it does |
|-------|--------------|
| `src/Host/appsettings.Production.json` | Public endpoint on `http://+:8080`; the four database paths under `/home/data/sprint-rituals/`. No secrets. |
| `deploy/deploy.ps1` | Build (`npm ci`, `npm run build`, `dotnet publish`) → `artifacts/deploy.zip` → `az webapp deploy` → smoke check. Modes `-BuildOnly`, `-SkipBuild`, `-SmokeOnly -BaseUrl`. |
| `deploy/provision.ps1` | `-Prepare` (read-only report) and `-Cutover` (the one-time configuration change). `-DryRun` prints every `az` command and runs none. |
| `.github/workflows/ci.yml` | Build, tests, client build and a local smoke check on every push and pull request. |
| `.github/workflows/deploy.yml` | Manual deploy (`workflow_dispatch` only): a build job, then a deploy job that signs in to Azure with OIDC. |

Both scripts run under Windows PowerShell 5.1 and PowerShell 7. On Windows, start them as `powershell.exe -ExecutionPolicy Bypass -File deploy\<script>.ps1 …` if your execution policy blocks local scripts.

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli), signed in with `az login` to the work tenant, on the subscription (`az account set --subscription "<subscription>"` if you have several).
- For laptop deploys: Node 24 and the .NET 10 SDK.
- PowerShell 7 is optional — Windows PowerShell 5.1 runs both scripts.
- The local target file: copy `deploy/deploy-config.example.ps1` to `deploy/deploy-config.ps1` (git-ignored) and check its values (`<app-name>`, `<resource-group>`). Without it, pass `-AppName` and `-ResourceGroup` to each script.
- A Google OAuth client (Client ID + Client Secret) in [Google Cloud Console](https://console.cloud.google.com/).
- Jira: the instance URL, a service account's email and API token, and the project key.

## Zero cost

The app stays on the **F1 (Free)** plan; `provision.ps1` refuses to run on any other tier and no script or workflow ever changes the tier. F1's limits, accepted by the owner:

- **60 CPU-minutes a day**, one allowance for the whole suite. When it is used up the site stops until the next day's reset.
- **5 WebSocket connections** for all three products together. Past that, browsers fall back to server-sent events or long polling and keep working, just slower.
- **The site sleeps after about 20 idle minutes**, so the first request after a quiet spell is slow (the deploy smoke check waits up to 300 s for this reason).
- **No built-in backup.** The nightly database snapshot is a separate feature, **F10** (owner-deferred). Until then, copy `/home/data/sprint-rituals/` by hand (Kudu) before risky changes.

## Demo day

For a demo with more than about 5 people live, or one longer than a short walkthrough, scale the plan `<plan-name>` to B1 for that day only.

1. About 30 minutes before, from the laptop under your own `az login` (the GitHub deploy identity has no rights on the plan, on purpose):

   ```powershell
   az appservice plan update --name <plan-name> --resource-group <resource-group> --sku B1
   ```

   Every app on that plan moves with it (`provision.ps1 -Prepare` lists them), and each restarts once. If the app had to go on `<old-plan-name>` (see Prepare step 1), use that name instead.
2. Confirm: `az appservice plan show --name <plan-name> --resource-group <resource-group> --query sku.name -o tsv` prints `B1`. Open each of the three apps (`/dashboard/`, `/retro/`, `/poker/`) once to warm them up.
3. Leave **Always On** off — Free cannot run it, so it would block the scale-back.
4. The same day, after the demo:

   ```powershell
   az appservice plan update --name <plan-name> --resource-group <resource-group> --sku F1
   az appservice plan show --name <plan-name> --resource-group <resource-group> --query sku.name -o tsv   # F1
   ```

   B1 costs roughly $0.02 an hour against the Sponsorship credit (list price, unverified); a forgotten scale-back costs about $13 a month. `provision.ps1` refuses to run while the plan is on B1, which is a reminder too.

Without the rights or the credit to scale: keep the live audience to about 5 people, and let the others watch a shared screen.

## Growing beyond free

What each later need changes — none of it is built now, and the scripts, workflows and config files stay as they are for all of these:

- **No CPU quota, Always On, backups** → a standing B1 (same commands as Demo day). The `F1` guard in `provision.ps1` is dropped only by a new ADR.
- **More than one instance** → RabbitMQ transport, a SignalR backplane and a managed database first (ADR-024 § Single instance).
- **Lock errors on `/home`** → a managed database (ADR-024 § Storage).
- **A nicer address** → a custom domain.

## Prepare (nothing users can reach yet)

Nothing in this section changes `<old-fokus-app>`. The new app exists after step 1 but serves only Azure's placeholder page until the Cutover deploy.

1. **Create the free plan and the app** (the runtime string comes from `az webapp list-runtimes --os linux`):

   ```powershell
   az appservice plan create --name <plan-name> --resource-group <resource-group> --location polandcentral --is-linux --sku F1 -o none
   az webapp create --name <app-name> --resource-group <resource-group> --plan <plan-name> --runtime "DOTNETCORE:10.0" -o none
   ```

   If Azure refuses a second free Linux plan, skip the plan and create the app with `--plan <old-plan-name>` instead. HTTPS-only is set later by `-Cutover`.
2. **Google:** on the existing OAuth client, add the redirect URI `https://<app-name>.azurewebsites.net/auth/callback` (and the JavaScript origin `https://<app-name>.azurewebsites.net`), keeping every existing URI.
3. **OIDC app registration** in the work tenant (needs the right to create app registrations):

   ```powershell
   az ad app create --display-name <deploy-identity-name> --query appId -o tsv   # <appId>
   az ad sp create --id <appId> -o none
   Set-Content -Path credential.json -Value '{"name":"github-production","issuer":"https://token.actions.githubusercontent.com","subject":"repo:<github-owner>/<repo>:environment:production","audiences":["api://AzureADTokenExchange"]}'
   az ad app federated-credential create --id <appId> --parameters "@credential.json" -o none
   Remove-Item credential.json
   az role assignment create --assignee <appId> --role "Website Contributor" --scope /subscriptions/<subscriptionId>/resourceGroups/<resource-group>/providers/Microsoft.Web/sites/<app-name> -o none
   ```

   The federated credential trusts only the `production` environment of this repository (subject `repo:<github-owner>/<repo>:environment:production`). **Website Contributor** is scoped to the one site: it can deploy, but cannot touch the plan, so a tier change is always a person's act. If you cannot create the app registration, the laptop deploy path (`deploy.ps1`) still works; record that in the feature's `implementation.md`.
4. **GitHub** (repository settings):
   - create the environment `production`, with deployment branches limited to `main`;
   - add five repository **variables** (not secrets — none of them is secret): `AZURE_CLIENT_ID` (`<appId>`), `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_WEBAPP_NAME` (`<app-name>`), `AZURE_RESOURCE_GROUP` (`<resource-group>`).
5. **Jira:** have the instance URL, the service account's email and API token, and the project key ready.

## Cutover (one sitting)

The cutover deploy runs from the laptop, because the GitHub runner is outside the allowed IP; later deploys use the workflow.

1. **Stop the old Fokus app** — the backup; stop only, never delete or change it:

   ```powershell
   az webapp stop --name <old-fokus-app> --resource-group <resource-group>
   az webapp show --name <old-fokus-app> --resource-group <resource-group> --query state -o tsv   # Stopped
   ```

2. **Restrict the new app to your own IP:**

   ```powershell
   curl.exe -s https://api.ipify.org                       # <your-ip>
   az webapp config access-restriction add --name <app-name> --resource-group <resource-group> --rule-name owner-only --action Allow --ip-address <your-ip>/32 --priority 100 -o none
   ```

   Confirm from another network (for example a phone on mobile data) that `https://<app-name>.azurewebsites.net` answers **403**. **If the restriction cannot be applied or confirmed, stop here** — never start the new app publicly with an empty user table: the first Google user to sign in becomes Admin.
3. **Run the read-only report:**

   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File deploy\provision.ps1 -Prepare
   ```

   It checks the plan is F1, prints the **plan name** and **every app on the plan**, and the missing required settings by name (`Google__ClientId`, `Google__ClientSecret`, `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey`) and the missing optional ones (`Xray__ClientId`, `Xray__ClientSecret`, `Xray__BaseUrl`). On a new app every one of them is missing, and the startup command and `Database` path are empty.
4. **Apply the new configuration**, then the missing settings:

   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File deploy\provision.ps1 -Cutover
   ```

   It sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `SCM_DO_BUILD_DURING_DEPLOYMENT=false`, `Auth__AllowedReturnOrigins__0=https://<app-name>.azurewebsites.net`, container logging to the filesystem, HTTP/2, HTTPS-only, startup command `dotnet Host.dll`, and removes any `Database` connection string; every step is idempotent, so a failed run is just run again. It first writes `deploy/.cutover/old-config.txt` (git-ignored); on a new app that file records only empty values and is not needed for rollback, but a re-run from another checkout needs the same file, because `-Cutover` refuses when the startup command is already `dotnet Host.dll` and no snapshot exists.

   Then set every setting `-Prepare` reported missing — Google and Jira included — in the portal (App Service → Environment variables), or with `az webapp config appsettings set --name <app-name> --resource-group <resource-group> --settings <Name>=<value> -o none` (the value then sits in your shell history; the portal avoids that).
5. **Deploy from the laptop:**

   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File deploy\deploy.ps1
   ```

   Its smoke check runs from your allowed IP and waits until `/` redirects to `/dashboard/login`.
6. **Sign in with Google first**, then confirm that the Admin user and the company domain are yours.
7. Run the **Smoke test** below while still restricted.
8. **Remove the access restriction:**

   ```powershell
   az webapp config access-restriction remove --name <app-name> --resource-group <resource-group> --rule-name owner-only -o none
   ```

If a foreign Admin or company domain appears: keep the restriction, delete every file in `/home/data/sprint-rituals/` (all four databases — the company domain lives in Fokus's; Kudu SSH: `rm /home/data/sprint-rituals/*`), restart the site (`az webapp restart --name <app-name> --resource-group <resource-group>`), and sign in again. Nothing worth keeping exists there yet.

## Smoke test

Record each result in the feature's `implementation.md` as an `OPERATOR ACTION` line:

- `/` redirects to `/dashboard/login`.
- The Google sign-in URL's `redirect_uri` starts with `https://` (it ends in `/auth/callback`), and the auth cookie is marked Secure.
- On the SSH console (Kudu, `https://<app-name>.scm.azurewebsites.net`), `ls -la /home/data/sprint-rituals/` shows `identity.db`, `fokus.db`, `reflekt.db` and `poker.db`. `<old-fokus-app>` is stopped, not deleted (`az webapp show --name <old-fokus-app> --resource-group <resource-group> --query state -o tsv` prints `Stopped`).
- Run a Fokus sprint sync against the team's Jira. Developers appear, and the log has no Person-upsert warning.
- Create a retro. The sprint picker lists the synced sprints (sprint catalog over gRPC), and its sprint-context panel loads (sprint analytics over gRPC).
- Create a Poker sprint session. Its tickets load (Poker to Fokus over gRPC).
- Open a Poker room as a guest in a second browser. Votes appear live, and the browser network tab shows a WebSocket upgrade (101).
- Create an invitation. The link starts with `https://<app-name>.azurewebsites.net/invite/`, and a second Google account can redeem it.
- Restart the site. Data is still there.

Logs: `az webapp log tail --name <app-name> --resource-group <resource-group>` (container logging to the filesystem is switched on by `-Cutover`).

## Connecting Jira and inviting the team

- The Jira values are App Service settings set in Cutover step 4: `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey` (and `Jira__IsTeamManaged` only if your project is company-managed: `false`). Without them the app starts on the placeholder values in `appsettings.json` and every sync fails.
- The first sprint sync in Fokus pulls the sprint's developers; each one becomes a Person in Identity (over gRPC).
- Invitations come from Fokus (create an invitation → share the `/invite/…` link); the invitee signs in with Google to redeem it.

## Reflekt gate

Existing Reflekt users stay on the old personal-account app (`<old-reflekt-app>.azurewebsites.net`, RG `<old-resource-group>`) until **F9** has migrated their data. F8 never changes that app — no script, workflow or step here targets it.

## Later deploys

- **GitHub:** Actions → *Deploy* → *Run workflow* on `main`. The `build` job runs the tests and `deploy.ps1 -BuildOnly`; the `deploy` job (environment `production`) downloads the zip, signs in with OIDC and runs `deploy.ps1 -SkipBuild`, including the smoke check. Only one deploy runs at a time.
- **Laptop:** `powershell.exe -ExecutionPolicy Bypass -File deploy\deploy.ps1`.
- App settings persist between deploys; nothing else to change.

## Rollback

The old Fokus app was only stopped, so rollback is switching back:

1. Stop the new app — `az webapp stop --name <app-name> --resource-group <resource-group>` — or, to drop it entirely, `az webapp delete --name <app-name> --resource-group <resource-group>` (its databases go with it).
2. Start the old app: `az webapp start --name <old-fokus-app> --resource-group <resource-group>`, then confirm `az webapp show --name <old-fokus-app> --resource-group <resource-group> --query state -o tsv` prints `Running`.

`<old-fokus-app>` keeps its own settings, files and database, so it comes back as it was. Retiring it for good is a later decision, once the new app is proven.

## Single-instance rule

The suite runs on **exactly one instance**: SQLite files on `/home`, the in-memory MassTransit bus and SignalR without a backplane all assume one process. Never scale out (F1 cannot anyway); see Growing beyond free for what comes first.
