# Sprint Rituals

Sprint Rituals is a suite of three tools for a Scrum team's sprint rituals, run as one modular monolith with one sign-in:

- **Fokus** — sprint analytics: syncs sprints, issues and test executions from Jira (and Xray) and shows health, cycle time, bug ratio, carry-over and per-developer metrics.
- **Reflekt** — real-time retrospectives: writing, grouping, voting, discussion and action items, with live updates for everyone in the room.
- **Poker** — real-time planning poker for estimating the sprint's tickets.

## Why this repo exists

Every line of code, every test and every document here was written by AI agents — a product owner, an architect, a developer, reviewers and a learner — working through a fixed pipeline, with a human owner making the decisions. The architecture follows the author's .NET microservices course repository (DDD, vertical slices, CQRS, Clean Architecture, event-driven design), applied here as one deployable host made of independent modules. The repository keeps the full record of how it was built; see [How it was built](#how-it-was-built).

## Stack

- **Backend:** .NET 10, ASP.NET Core, FastEndpoints, FluentValidation, Mapster, EF Core with SQLite (one database per module), MassTransit (in-memory transport), gRPC code-first (protobuf-net.Grpc) between modules, SignalR for live updates, Google sign-in with cookie authentication, OpenAPI with Scalar.
- **Frontend:** Vue 3, TypeScript, Pinia, Tailwind CSS, Vite — three apps (dashboard, retro, poker) built into the host's `wwwroot`.

## Repository layout

```
src/
  Host/              the one web project; composes every module
  BuildingBlocks/    shared libraries (domain, EF Core, FastEndpoints, cross-module contracts)
  Services/          the modules: Identity, Fokus, Reflekt, Poker
  Modules/           integration clients: Jira, Xray
client/
  apps/              dashboard (Fokus), retro (Reflekt), poker (Poker)
  packages/shared    code shared by the three apps
deploy/              deploy and provisioning scripts
docs/                architecture, conventions, product docs, knowledge base and the build record
SprintRituals.slnx   the solution
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+ (what Vite 8 requires)
- A Google OAuth client (web application) for sign-in
- Optional: a Jira Cloud site and API token, for Fokus to sync sprint data

## Run locally

1. Clone the repository:

   ```bash
   git clone https://github.com/<github-owner>/sprint-rituals.git
   cd sprint-rituals
   ```

2. Set the secrets on the host project with `dotnet user-secrets` (never in a committed file). In the Google OAuth client, add the redirect URI `http://localhost:5100/auth/callback`.

   ```bash
   dotnet user-secrets set "Google:ClientId" "<google-client-id>" --project src/Host
   dotnet user-secrets set "Google:ClientSecret" "<google-client-secret>" --project src/Host

   # Optional — Jira, for Fokus's sprint sync
   dotnet user-secrets set "Jira:InstanceUrl" "https://<your-site>.atlassian.net" --project src/Host
   dotnet user-secrets set "Jira:Email" "<jira-account-email>" --project src/Host
   dotnet user-secrets set "Jira:ApiToken" "<jira-api-token>" --project src/Host
   dotnet user-secrets set "Jira:ProjectKey" "<project-key>" --project src/Host
   ```

3. Build the three client apps into the host's `wwwroot`:

   ```bash
   cd client
   npm ci
   npm run build
   cd ..
   ```

4. Run the host:

   ```bash
   dotnet run --project src/Host
   ```

   The host listens on `http://localhost:5100` and serves the apps at `/dashboard/`, `/retro/` and `/poker/`. It creates and migrates the four SQLite databases (`identity.db`, `fokus.db`, `reflekt.db`, `poker.db`) under `data/` on start. The first person to sign in becomes the administrator. The API reference (Scalar) is available in the Development environment.

For front-end work, run an app's Vite dev server instead of rebuilding — it proxies API calls to the running host:

```bash
cd client
npm run dev:dashboard   # http://localhost:5171/dashboard/
npm run dev:retro       # http://localhost:5172/retro/
npm run dev:poker       # http://localhost:5173/poker/
```

## Tests and client build

```bash
dotnet test SprintRituals.slnx        # every test project in the solution
cd client && npm ci && npm run build  # type-check and build the three apps
```

The tests need no Google or Jira credentials.

## Deploy

The whole suite deploys as one app to a free Linux Azure App Service. The scripts are in `deploy/` and the operator runbook is [`docs/deployment-azure.md`](docs/deployment-azure.md).

## How it was built

The suite was built feature by feature through an agent pipeline run with the [nexus plugins for Claude Code](https://github.com/DotNetLabX/claude-nexus) (enabled in `.claude/settings.json`):

1. a **product owner** agent shapes the feature with the human owner and writes the spec;
2. an **architect** agent writes the implementation plan, which a critic reviews;
3. a **developer** agent builds it step by step, following the plan and the plugins' skills;
4. the architect checks every planned step was done, and a **reviewer** agent reviews the code; findings go back to the developer;
5. the run closes with a summary, and a **learner** agent turns recurring lessons into rules and skills.

The record of all of this is kept in the repository:

- **`docs/specs/`** — one folder per feature or ad-hoc change: `definition/` holds the spec, `delivery/` holds the plan, the implementation log, the reviews, the questions asked, the lessons and the summary. `docs/specs/fokus/` and `docs/specs/reflekt/` hold each product's feature records, including those carried over from the original Fokus and Reflekt projects.
- **`docs/proposals/`** — design proposals and research written before larger changes.
- **`docs/architecture/decisions.md`** — the architecture decision register (ADRs), with the architecture pages beside it.
- **`docs/conventions/`** — the coding conventions and project rules the agents follow.
- **`docs/kb/`** — the knowledge base: business rules, formulas and edge cases.
- **`docs/product/`** and **`docs/backlog.md`** — product docs per product, and the backlog.

## License

MIT — see [LICENSE](LICENSE).
