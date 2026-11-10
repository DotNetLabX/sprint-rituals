# Proposal — Deploy SprintRituals to Azure, replacing the standalone Fokus (and later Reflekt)

**Status:** Ratified 2026-10-05 — graduated to `F8-AzureDeployment` (ADR-024). Amended at ratification: CI/CD **in** scope (manual-trigger GitHub workflow), SQLite journal mode switched for `/home`, old Reflekt data carried over by `F9`.
**Decision-maker:** Laurentiu Dumitrescu (owner)
**Recommendation:** Port the original Fokus `deploy.ps1` zip-deploy pattern to this repo and deploy the single Host onto the existing work-account App Service `<old-fokus-app>`, with the new databases on a fresh `/home/data/sprint-rituals/` path so the old Fokus data is kept untouched as a backup.
**Confidence:** Medium — the pattern is proven by old Fokus/Reflekt, but three load-bearing facts are unverified: the in-container gRPC loopback (port 5150) on App Service, the Google redirect over App Service's TLS proxy, and whether the old Fokus data needs carrying over.
**Impact:** 8
**Effort:** med
**Date:** 2026-10-05

## Need

SprintRituals (Fokus + Reflekt + Poker + Identity in one Host) runs locally but has **no deploy path** —
no script, no pipeline, no Azure config in this repo. The owner wants it to **replace** the live
standalone apps:

| Live app | Azure account | Replace when |
|---|---|---|
| `<old-fokus-app>` (`<old-fokus-app>.azurewebsites.net`) | **Work** — the work tenant, the subscription, RG `<resource-group>` | **First** (this proposal) |
| Reflekt (original) | **Personal** account | Later — not in the work tenant |

One SprintRituals deployment replaces both, so it lands in **one** account only. Work account first.

**Out of scope:** CI/CD (GitHub Actions), containers, custom domain, Postgres, the personal-account move,
migrating old Reflekt data.

## Approach

### Live state of `<old-fokus-app>` (read 2026-10-05, read-only)

| Setting | Value |
|---|---|
| Kind / runtime | Linux App Service, `DOTNETCORE|10.0` |
| Plan | `<old-plan-name>`, **F1 (Free)** |
| Startup command | `dotnet Fokus.API.dll` |
| WebSockets / HTTP2 / AlwaysOn / HTTPS-only | on / on / off / **off** |
| App settings (names only) | `Google__ClientId`, `Google__ClientSecret`, `Jira__InstanceUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__ProjectKey`, `SCM_DO_BUILD_DURING_DEPLOYMENT`, `WEBSITE_HTTPLOGGING_RETENTION_DAYS` |
| Connection strings | `Database` (Custom) — per Fokus `docs/deployment-azure.md`: `Data Source=/home/data/fokus.db` |

### Steps for the deploying session

1. **Back up the live Fokus DB first.** Download `/home/data/fokus.db` (Kudu/SSH or `az webapp`
   file download) and keep it outside Azure. Nothing below should touch it, but it is the only copy.
2. **Add `deploy.ps1` at repo root**, adapted from `<fokus-repo>/deploy.ps1` (27 lines):
   `cd client; npm ci; npm run build` (builds all three SPAs into `src/Host/wwwroot/{dashboard,retro,poker}`)
   → `dotnet publish src/Host -c Release -o publish` → zip → `az webapp deploy --type zip --clean true`.
   Read config (app name, RG, subscription) from a git-ignored `deploy-config.ps1` like Fokus does.
3. **Add `docs/deployment-azure.md`** — the one-time setup below, mirroring Fokus's guide.
4. **One-time App Service config** (work account, `<old-fokus-app>`):
   - Startup command → `dotnet Host.dll`.
   - **Kestrel binding:** `appsettings.json` pins `Kestrel:Endpoints:Http:Url = http://localhost:5100`,
     which overrides App Service's port. Set `Kestrel__Endpoints__Http__Url = http://0.0.0.0:8080`
     (App Service's default) — or `WEBSITES_PORT` to match — and keep the gRPC endpoint on
     `http://localhost:5150` (in-container loopback; **verify**, see Unresolved).
   - **Connection strings — new paths, never the old file:**
     `IdentityDatabase = Data Source=/home/data/sprint-rituals/identity.db`,
     `Database = Data Source=/home/data/sprint-rituals/fokus.db`,
     `ReflektDatabase = …/reflekt.db`, `PokerDatabase = …/poker.db`.
     ⚠ The existing `Database` string points at the **old** `/home/data/fokus.db`; the new Fokus
     migrations against that file would collide with the old schema. Overwrite it.
     (`DbContextExtensions` creates the folder at startup.)
   - **Auth:** add `Auth__AllowedReturnOrigins__0 = https://<old-fokus-app>.azurewebsites.net` (else
     post-login return falls back to `/dashboard/`). Google console: authorized redirect URI
     `https://<old-fokus-app>.azurewebsites.net/auth/callback` (same path as old Fokus).
   - Keep the `Jira__*` and `Google__*` settings. Add `Xray__ClientId`, `Xray__ClientSecret`,
     `Xray__BaseUrl` if the test-metrics tab is wanted (old app has none).
   - Turn **HTTPS-only on**.
5. **Plan size:** move `<old-plan-name>` **F1 → B1** (~$13/mo). F1 caps CPU at 60 min/day, has no
   AlwaysOn, and limits concurrent WebSocket connections — Reflekt retros and Poker rooms are
   real-time (SignalR) for a whole team at once.
6. **Deploy, then smoke test:** `/` redirects to `/dashboard/login`; Google sign-in returns to the app;
   run a Fokus sprint sync; create a retro (sprint picker lists synced sprints); create a Poker sprint
   session (tickets load); open one room as a guest in a second browser.
7. **Reflekt (personal account) — later:** after the suite is live, point users of the old Reflekt to
   the new `/retro/` URL; decommissioning the personal app is a separate step.

## Benefits

- One deployable replaces two separately-hosted apps; one sign-in across Fokus, Reflekt and Poker.
- Reuses a deploy shape the owner already operates (Fokus/Reflekt `deploy.ps1`) — no new tooling.
- The old Fokus database stays on disk untouched, so rollback = redeploy old Fokus + restore its
  connection string.

## Alternatives

- **New App Service instead of replacing `<old-fokus-app>`** — zero risk to the live app and a clean
  side-by-side test, but a new URL and a second Google redirect URI. Owner chose replacement.
- **Knowledge-gateway pattern** (GitHub Actions → GHCR images → Azure Container Apps, numbered
  idempotent provisioning scripts) — production-grade, but SQLite files need mounted storage in
  containers, and the provisioning surface (Front Door, Key Vault, Postgres) is far beyond what one
  internal app needs today. A sensible later step if the suite moves to Postgres.
- **GitHub Actions running the same zip deploy** — automates step 2; needs an Azure service principal
  or OIDC federation in the work tenant. Worth doing once the manual path is proven.

## Unresolved

1. **gRPC loopback on App Service** — the Host calls itself on `http://localhost:5150` (h2c) for
   cross-module gRPC (Reflekt → Fokus sprint catalog/analytics, Poker → Fokus, Identity ↔ Fokus).
   Expected to work inside the container, but unverified on App Service Linux. If it fails: sprint
   picker, sprint context, Poker sprints and invitations break while the rest works. Verify in the
   smoke test (step 6).
2. **Google redirect over TLS** — the Host has no `UseForwardedHeaders`, so behind App Service's
   proxy the OAuth `redirect_uri` may be built as `http://`. Old Fokus had the same code and worked,
   so the Google client probably lists the `http://` URI; with HTTPS-only on, add forwarded-headers
   handling or register both. Verify.
3. **Old Fokus data** — does anything in the old `fokus.db` need carrying over (developer capacities,
   invitations, company domain, test-metric history)? Sprint/ticket data re-syncs from Jira. The new
   schema differs, so a carry-over is a migration script, not a file copy. Owner decision.
4. **First admin, then invitations** — Identity makes the first Google user to sign in the Admin and
   seeds the company domain from their email, so the owner signs in first. Every later Google user
   needs an **invitation** (Fokus invitations) — a regular login with no User record is rejected. Old
   Fokus users do not carry over; plan to re-invite the team. (Retro/Poker guests need no account.)

## Graduate-to-spec

Technical proposal → on ratification, the deploying session takes it as a feature slug with a plan
(architect or team lead), producing `deploy.ps1`, `docs/deployment-azure.md`, and an ADR for the
hosting choice.

## Provenance

Solo session `adhoc-run-app`, 2026-10-05. Inputs: read-only `az` queries against `<old-fokus-app>`
(names and flags only, no secret values); `<fokus-repo>/deploy.ps1` and `docs/deployment-azure.md`;
`<reflekt-repo>/deploy.ps1`; `<private-repo>/deploy/*` and `.github/workflows/images.yml`;
this repo's `src/Host/appsettings.json` and `Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs`.
