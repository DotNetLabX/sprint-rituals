# F8-AzureDeployment — Critic record

## Round 1 — 2026-10-05 — plan — primary (codex)

Header note: primary is Codex — its verdict and findings are in `review-critic-codex.md` (NO-GO, 12 findings). Second reader: omni critic on Sonnet. Both read the full plan, ADR-024 and the baseline surfaces; neither read the live Azure tenant or GitHub repo settings. The second reader's own `##` headings are kept as written, except "Round-0 fold verification", which is demoted to `###` so it cannot be read as a round heading.

## Second reader — round 1 (sonnet)

# F8 — Azure Deployment — Critic Review (second reader, `critic-second`)

## Mode: Plan Review
## Verdict: REVISE

No CRITICAL finding. Two findings are suggested HIGH or HIGH-leaning: S1 (deploy identity exposed to build-time code) and J1 (journal-mode premise and ordering). Both are fixable inside the plan.

Most findings are ungraded because the seed ruler (a document-review corpus) has no anchor for deployment, security or data-safety shapes. Each carries `### [UNRANKED]` and a reader-suggested level in its Issue text, as the calibration rule requires. The verdict is computed from those suggested levels.

## Pre-commitment Predictions
| Predicted | Found |
|---|---|
| Kestrel pin vs the platform port | Handled: Step 1 pins 8080. Explicit `Kestrel:Endpoints` in `src/Host/appsettings.json:2-6` does override `ASPNETCORE_HTTP_PORTS`. |
| App Service connection string overriding the JSON paths | Correctly identified. The *proof* of the delete is a dry-run print only (F-A). |
| WAL→DELETE switch mechanics on a network mount | Gaps found (J1, M2). |
| OIDC and workflow security | Gap found (S1), plus a first-admin race (S3) and secret echo (S2). |
| Acceptance lines not mechanical | Several found (C1, P1). |

Unpredicted: the missing ADR "Backups" step (D1), the missing old-Fokus rollback artifact (F-B), and the missing `artifacts/publish` producer in the deploy workflow (P1).

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed only (no `docs/critic-calibration/ruler.md` on disk; Glob returned nothing) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| ADR-024 Target: B1, one instance, AlwaysOn, HTTPS-only | decisions.md § ADR-024; plan Step 4 | COVERED |
| ADR-024 Single instance binding | plan Step 4 (instance count 1), Step 6 runbook rule | COVERED |
| ADR-024 Storage: `/home/data/sprint-rituals`, DELETE mode set by config | plan Steps 1–2; `DbContextExtensions.cs`; 4× `*.Persistence/DependencyInjection.cs` (`UseSqlite(connString)` only); local `.db` headers read (bytes 18–19 = `2 2` ×4) | PARTIAL: ordering and verification (J1, M2) |
| ADR-024 Backups: hourly automatic backup | decisions.md:131; `grep -n backup plan.md` (only lines 108, 111, about the old file) | MISSING (D1) |
| ADR-024 Non-secret config is code; secrets in App Service | plan Steps 1, 4; `appsettings.json`; `JiraOptions.cs` | COVERED |
| ADR-024 `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | plan Step 4; `AuthConfiguration.cs:42-51`; `CreateInvitationEndpoint.cs:39` | COVERED; invite-link check is missing (I1) |
| ADR-024 Deploy path: one script, CI on every push, manual dispatch, OIDC | plan Steps 3, 5; `.gitignore`; `client/package.json`; vite configs; `SprintRituals.slnx` | PARTIAL (P1, S1) |
| ADR-024 Data carry-over (Fokus not migrated, Reflekt = F9) | plan Scope/Out of scope | COVERED |
| Proposal: gRPC loopback on :5150 | `appsettings.json:8-16`; `Fokus.API/DependencyInjection.cs:114`; plan Step 9 | COVERED (live-only, accepted-gap) |
| Proposal: Google redirect over TLS | `AuthConfiguration.cs:45`; no `UseForwardedHeaders` in `src` (grep) | COVERED |
| Proposal: first admin, then invitations | `AuthConfiguration.cs:80-84, 179-206`; plan Step 9 | PARTIAL (S3) |
| Proposal: back up old Fokus DB first | plan Step 7; proposal line 38 | PARTIAL (F-A, F-B) |
| Context facts (plan lines 14–21) | all re-verified: appsettings, Development.json, Program.cs, Host.csproj (no AssemblyName, so `Host.dll`), slnx (no BuildingBlocks tests), no `.github`, `client/package-lock.json` tracked | COVERED, all true |
| Baseline `dotnet test SprintRituals.slnx -c Release` (executed, isolated artifacts path) | exit 0; 339 tests passed (26+59+224+5+6+19), 0 failed | COVERED: CI will not start red |
| `dotnet publish src/Host` (executed) | `Host.dll` present; published `appsettings.json` is byte-identical to Host's, not Fokus.API's; `wwwroot/{dashboard,retro,poker}` present; `appsettings.*.json` files are published by default glob | COVERED |
| Old `/home/data/fokus.db` untouched by new code paths | Persistence DI (4 connection-string keys); `--clean` scope is `/home/site/wwwroot`, not `/home/data` | PARTIAL (F-A) |
| Reflekt.Persistence.Tests stack, `Directory.Packages.props` | read | COVERED |
| `CLAUDE.md` | read via injected context | COVERED |
| PowerShell 7 on this machine | `where.exe pwsh` none; `C:\Program Files\PowerShell` absent; only 5.1.26100 | gap (C1) |
| Live `<old-fokus-app>` state (`az` read-only queries) | — | NOT OPENED: brief scoped to repo files; no work-tenant login used |
| GitHub repo visibility / settings | — | NOT OPENED: `gh` CLI is not installed; visibility unknown |

### Round-0 fold verification
Not applicable (round 1).

## Findings

### [UNRANKED] S1 — Deploy identity is reachable from a job that runs `npm ci`; the `production` environment has no stated gate
**Anchor:** unranked — no anchor fits; persona grades
**Band:** MEDIUM–HIGH; driver: whether anyone besides the owner can run workflows or push branches. Reader's suggested level: **HIGH** (security class, never downgrade).
**Shape:** least-privilege / supply-chain exposure in CI
**Consequence:** Code run during the dependency install can mint the deploy identity's token, so a poisoned package could deploy code or read production secrets.
**Evidence:** quoted — plan.md:100 `permissions: id-token: write, contents: read; GitHub environment production; … build + test as CI, then azure/login`; plan.md:117 "add the GitHub variables and the `production` environment" (no protection rules); plan.md:161 role is Website Contributor (`Microsoft.Web/sites/*`, which includes reading app settings, i.e. Google and Jira secrets, and deploying); plan.md:162 rejects "cross-workflow artifact plumbing" as the reason for a single job.
**Surfaces:** plan.md § Step 5, § Step 8, § Decisions; `client/package.json` (≈15 direct deps; hundreds transitive; lifecycle scripts run on `npm ci`)
**Untouchable:** security
**Source:** plan Step 5. **Issue:** `permissions` is job-wide. The OIDC token is mintable by any process in the job, so `npm ci` and `npm run build` run with the production identity available. This is the pattern in the 2026 TanStack CI compromise reported in the search results. The environment's deployment-branch and reviewer rules are never specified, so a `workflow_dispatch` on any branch (modified `deploy.ps1` or workflow) receives the `environment:production` subject. **Impact:** a poisoned dependency or an unreviewed branch run gets deploy rights plus read of production secrets.
**Fix:** (1) Split `deploy.yml` into two jobs in the same workflow. `build` has `contents: read` only and runs npm, dotnet build/test and `dotnet publish`, then `upload-artifact artifacts/publish`. `deploy` has `needs: build`, `environment: production`, `id-token: write`, downloads the artifact, runs `azure/login`, then `deploy.ps1 -SkipBuild`. This is intra-workflow, so the rejected "cross-workflow plumbing" does not apply, and it also closes P1. (2) Step 8 and the runbook set the `production` environment's deployment branches to `main` only, plus an optional required reviewer. (3) Pin `azure/login` to a commit SHA. (4) Add a mechanical acceptance line: `id-token: write` appears exactly once, under the `deploy` job (`yq '.jobs.build.permissions'` has no `id-token`).

### [UNRANKED] J1 — The journal-mode premise and ordering are unproven: creation and migration run in WAL on the share first, and Step 9 cannot falsify DELETE
**Anchor:** unranked — no anchor fits; persona grades
**Band:** MEDIUM–HIGH; driver: whether `PRAGMA journal_mode=wal` at create time and later `DELETE` locking both work on the `/home` CIFS mount, which is knowable only live. Reader's suggested level: MEDIUM at the low end, HIGH if either fails.
**Shape:** unverified environmental premise plus ordering
**Consequence:** A database is created and migrated in the mode the plan calls unsupported on the share, and one-user testing cannot show lock errors.
**Evidence:** quoted — plan.md:17 "SQLite WAL is unsupported on network filesystems; App Service `/home` is a CIFS mount"; plan.md:67 switch "after `Database.Migrate()`"; plan.md:129 "created fresh … then switched to `DELETE` mode by Step 2"; `DbContextExtensions.cs:20-21` (`EnsureDataDirectory` then `Migrate()`); the plan's own verified fact that EF creates every file as WAL (headers `2 2` ×4). Smoke list plan.md:120 contains no concurrent-writer step.
**External (verified by search, not recalled):** a Microsoft Q&A thread (Node.js sqlite3 on App Service Linux, https://learn.microsoft.com/en-us/answers/questions/318948/azure-app-service-nodejs-sqlite3-lock-issue-cifs-m) reports `/home` is CIFS that "can not deal with SQLite3 lock" (SQLITE_BUSY, `nobrl` not user-changeable) and that `PRAGMA journal_mode=wal` fixed it. That is the opposite of ADR-024's premise. It is a secondary, anecdotal source, so it is a risk signal, not a refutation.
**Surfaces:** plan.md § Step 2, § Step 9, § Migration Notes; `DbContextExtensions.cs` § Migrate; `Identity.Persistence/DependencyInjection.cs:20` (and 3 siblings)
**Source:** ADR-024 Storage clause; plan Step 2. **Issue:** (a) First boot runs every migration in WAL on the share, then flips. If WAL on CIFS fails, the app crashes before the switch ever runs. If it works, the premise that WAL is unsafe there is weakened. Either way the design needs both modes to work on the mount. (b) The only live evidence, Step 9 (one owner plus one guest), cannot surface intermittent lock errors under two simultaneous writers (a Poker vote storm during a Fokus sync). (c) The fail-closed exception turns any switch failure into a startup crash of the replaced live app. (d) Nothing tells the operator that `Sqlite__JournalMode` can be overridden as an App Service app setting (env wins over JSON) as a no-redeploy fallback; the plan allows `WAL` as a value but never says so. **Impact:** possible first-boot crash, or intermittent lock errors discovered only after the team is on it, with rollback blocked by F-B.
**Fix:** (1) Settle the order in Step 2: apply the pragma before `Migrate()` so the file is never created WAL on the mount. A zero-byte pre-created file makes EF's `Exists()` true and skips WAL-on-Create; the executor confirms with the temp-file test. (2) Add to Step 9 a two-writer probe: two browsers voting in a Poker room while a Fokus sprint sync runs, with zero `database is locked` entries. (3) The runbook documents the fallback, `az webapp config appsettings set … Sqlite__JournalMode=WAL`, and the decision rule for it. (4) Route to the architect and owner: this premise is ADR-024's, and a contested external datum should be recorded in the ADR as a known risk.
**Route:** owner/architect for item 4; items 1–3 are plan-level.

### [UNRANKED] D1 — The ADR "Backups" clause has no plan step
**Anchor:** unranked — no anchor fits; persona grades (nearest shape: seed-0364, a stated requirement no step carries, graded LOW there for a lower-stakes consequence)
**Band:** MEDIUM–HIGH; driver: whether the Basic-tier automatic backup is on and restores `/home/data` for a Linux B1 app. Reader's suggested level: MEDIUM at the low end.
**Shape:** governing-decision clause not delivered by any step
**Consequence:** A decision-mandated safety net (hourly backup) has no step to enable or verify it, so data loss on the share may have no restore point.
**Evidence:** quoted — decisions.md:131 "Backups: App Service's built-in hourly automatic backup (Basic tier)". Plan Scope (line 25), Steps 1–9 and the Decisions table never mention it. `grep -n backup plan.md` hits only lines 108 and 111, about the *old* `fokus.db`. Microsoft Learn (manage-backup) confirms automatic hourly backups on Basic with 30-day retention and production slot only. It does not say whether Linux apps or `/home/data` are covered. I could not confirm that from the pages returned, so it is unverified.
**Surfaces:** decisions.md § ADR-024 Storage; plan.md § Scope, § Step 4, § Step 6
**Untouchable:** data-loss
**Source:** ADR-024. **Issue:** the plan does not silently de-scope the clause, it omits it. Neither `provision.ps1` nor the runbook enables or checks it, and no step exercises a restore. **Impact:** the only copy of the new Identity, Reflekt and Poker data sits on a network share in rollback-journal mode with an unverified restore path.
**Fix:** Step 4 reports (read-only, `--query` limited) the backup configuration status. Step 6 runbook adds "confirm automatic backup is enabled and note the restore procedure". Step 9 adds "a restore point exists after first deploy". If Linux or `/home/data` is not covered, record that and add a manual `/home/data/sprint-rituals` copy to the runbook.

### [UNRANKED] F-A — The "old file never opened" invariant is marked *pinned* but its Step 4 pin is a dry-run print, and the old path is doc-derived
**Anchor:** unranked — no anchor fits; persona grades
**Band:** MEDIUM–HIGH; driver: whether the live `Database` value really is `/home/data/fokus.db`. Reader's suggested level: MEDIUM.
**Shape:** invariant overclaimed as pinned
**Consequence:** A step meant to protect the old data file is only dry-run tested, so a missed override would let the new app change the one rollback copy.
**Evidence:** quoted — plan.md:136 "Old `/home/data/fokus.db` is never opened by the new app | Step 1 grep … + Step 4 deletes the overriding App Service connection string | pinned"; plan.md:94-95 Step 4 Accept is `-DryRun` plus a secret grep only; proposal line 38 "Database (Custom) — **per Fokus `docs/deployment-azure.md`**: `Data Source=/home/data/fokus.db`", i.e. never read from the live app. `Persistence/DependencyInjection.cs` of all four modules read `GetConnectionString("…")`, and App Service `CUSTOMCONNSTR_Database` overrides the JSON, so the plan's own mechanism analysis is right.
**Surfaces:** plan.md § Must-NOT-Change, § Step 4, § Step 7; `Fokus.Persistence/DependencyInjection.cs:20`; proposal § Live state
**Untouchable:** data-loss
**Source:** plan Step 4, Step 7. **Issue:** nothing checks live that `Database` is gone before the first boot. If the delete failed or was skipped, the new Fokus runs `Migrate()` against the old file and mutates the rollback copy; the plan's own proposal calls this a schema "collision". Step 7 backs up a doc-derived path. If the live value differs, the real data file is never backed up. **Impact:** the rollback copy can be altered while the plan reports the invariant "pinned".
**Fix:** `provision.ps1`, before deleting, writes the current `Database` value (path only, via `--query`) to the operator's console and a git-ignored file, and Step 7 backs up *that* path. It then asserts post-delete that `az webapp config connection-string list --query "[].name" -o tsv` contains no `Database`, and exits non-zero otherwise. Change the Must-NOT-Change disposition to "pinned by the post-condition", not the dry-run.

### [UNRANKED] F-B — Rollback depends on an artifact no step creates, and the live app stops serving as soon as Step 8 runs
**Anchor:** unranked — no anchor fits; persona grades
**Band:** MEDIUM–HIGH; driver: whether the live old-Fokus build is reproducible from the original Fokus project HEAD. Reader's suggested level: MEDIUM.
**Shape:** rollback precondition missing; cutover window unstated
**Consequence:** Rollback needs old binaries no step saves, and the live app stops serving as soon as setup runs, before any new build is deployed.
**Evidence:** quoted — plan.md:108 rollback "redeploy the old Fokus zip, startup command `dotnet Fokus.API.dll`, restore `Database = …`"; plan.md:113-114 Step 7 backs up only the DB; plan.md:89 Step 4 sets startup `dotnet Host.dll` and deletes `Database`; plan.md:79 deploy uses `--clean true`, which wipes `/home/site/wwwroot` (the old binaries). Verified absent: `<fokus-repo>/src/Services/Fokus/Fokus.API/deploy.zip` and `…\publish`.
**Surfaces:** plan.md § Step 4, § Step 6, § Step 7, § Step 9; `<fokus-repo>/deploy.ps1`
**Source:** plan Steps 6–9. **Issue:** no old zip exists on disk, and which commit is live is unrecorded. `provision.ps1` (Step 8) changes the startup command to a DLL that is not yet deployed and removes the connection string, so the old app fails on its next restart until Step 9 succeeds. The plan treats Steps 8 and 9 as independent. **Impact:** if the first deploy fails, an extended outage of the work app with a rollback that must first be reconstructed.
**Fix:** Step 7 also exports the live `/home/site/wwwroot` (Kudu zip download) and records the old startup command and the `Database` value. Steps 8 and 9 are stated as one back-to-back cutover with the downtime window named. Rollback in the runbook references that saved zip.

### [MEDIUM] P1 — Deploy workflow never produces the output `-SkipBuild` deploys
**Anchor:** seed-0130 · above seed-0127 · below seed-0116
(Placed above seed-0127, a wording ambiguity, because the first deploy run fails. Below seed-0116, wrong output published, because the failure is loud and pre-deploy and nothing ships.)
**Shape:** two plan statements that cannot both hold
**Consequence:** The deploy job runs a script told to skip building, but no step produces the files it deploys, so the first run fails.
**Evidence:** quoted — plan.md:81 "`-SkipBuild` (deploy an existing `artifacts/publish`)"; plan.md:100 deploy runs "build + test as CI, then … `deploy.ps1 -SkipBuild`", and CI (plan.md:99) runs `dotnet build`, `dotnet test` and `npm run build`, never `dotnet publish -o artifacts/publish`.
**Surfaces:** plan.md § Step 3, § Step 5
**Source:** plan Steps 3, 5. **Issue:** the CI recipe ends in a build, not a publish, so `artifacts/publish` does not exist when `deploy.ps1 -SkipBuild` runs. **Impact:** the first Step 9 run fails (loudly, before any `az` call, provided the script checks the folder exists).
**Fix:** subsumed by S1's two-job design: `build` runs `npm run build` then `dotnet publish src/Host -c Release -o artifacts/publish` and uploads it. The script fails fast with a clear message if `artifacts/publish/Host.dll` is missing under `-SkipBuild`. Add an acceptance line for that guard.

### [UNRANKED] S2 — "Never reads or writes secret values" is contradicted by `az` default output
**Anchor:** unranked — no anchor fits; persona grades
**Band:** LOW–MEDIUM; driver: whether the operator terminal or session transcript persists. Reader's suggested level: MEDIUM.
**Shape:** invariant violated by tool behaviour
**Consequence:** Config commands print every app setting, so live secret values land in the operator's terminal or transcript despite the never-read rule.
**Evidence:** quoted — plan.md:91 "Never reads or writes secret values. It reports, by name only, which required secret settings are missing". `az webapp config appsettings set` and `list` return the full settings including values by default; the fix, `-o none` or `--query`, is documented in the search results. The Step 4 grep only checks the script text, not command output.
**Surfaces:** plan.md § Step 4, § Must-NOT-Change "No secret value in git"
**Untouchable:** security
**Source:** plan Step 4. **Issue:** `provision.ps1` must call `appsettings set` (it sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED` and `SCM_DO_BUILD_DURING_DEPLOYMENT`) and `appsettings list` (to find missing secrets). Without output suppression it echoes the Google client secret and Jira API token. **Impact:** secrets leak to the operator's scrollback or an agent transcript.
**Fix:** every `az` call that can return settings or connection strings uses `-o none` (writes) or `--query "[].name" -o tsv` (reads), so values never leave the CLI. Add an acceptance line: `grep -nE "appsettings (set|list)|connection-string (set|list)" deploy/provision.ps1` shows each call with `-o none` or `--query`.

### [UNRANKED] S3 — First-admin bootstrap races the public site
**Anchor:** unranked — no anchor fits; persona grades
**Band:** MEDIUM–HIGH; driver: how long the site is reachable before the owner signs in. Reader's suggested level: MEDIUM.
**Shape:** exposure window at first boot
**Consequence:** Until the owner signs in, any visitor with a Google account becomes the permanent administrator of the live site.
**Evidence:** quoted — `AuthConfiguration.cs:80-84` `if (userCount == 0) { user = await HandleBootstrapAsync(...)` and `:194` `CreateFromPerson(person.Id, "Admin")`, with no domain restriction on the Google scheme (`AddGoogle` binds only ClientId/Secret, lines 42-46). Plan.md:120 orders "owner signs in with Google first" but states no gate or detection. The URL is the old Fokus URL, already known.
**Surfaces:** `Identity.API/Auth/AuthConfiguration.cs` § HandleTicketReceivedAsync/HandleBootstrapAsync; plan.md § Step 9
**Untouchable:** security
**Source:** plan Step 9, proposal Unresolved 4. **Issue:** the plan accepts the Identity design but leaves the deploy-to-first-login window open. The company-domain seed also derives from the first Admin's email. **Impact:** a stranger could become Admin and fix the company domain. This is detectable (the owner's own login then fails with `no-account`) and recoverable by wiping `identity.db`, but only if the operator notices.
**Fix:** in Step 9, immediately after the redirect check, the owner signs in and confirms the Admin user is theirs (Settings/users page). Optionally restrict access to the operator's IP for the first sign-in (`az webapp config access-restriction add`) and remove it after. If a foreign Admin exists, the runbook says to delete `/home/data/sprint-rituals/identity.db` and redeploy.

### [UNRANKED] C1 — Acceptance lines that cannot run as written
**Anchor:** unranked — no anchor fits; persona grades
**Band:** LOW–MEDIUM; driver: how soon the executor notices. Reader's suggested level: LOW–MEDIUM.
**Shape:** acceptance not mechanical
**Consequence:** Several acceptance lines cannot be executed on the build machine or name a switch the script contract never defines, so a pass proves less than claimed.
**Evidence:**
- plan.md:94 `pwsh deploy/provision.ps1 -DryRun`, and the PowerShell-7 requirement on Steps 3–4. PowerShell 7 is **not installed** on this machine (`where.exe pwsh` finds nothing; `C:\Program Files\PowerShell` is absent; Windows PowerShell 5.1.26100.9444 only). It is also not listed as a runbook prerequisite (plan.md:108).
- plan.md:85 "(`-SkipDeploy` or equivalent dry path, developer's choice of name)". No such switch is in the Step 3 contract (plan.md:77-81, only `-SkipBuild`).
- plan.md:63 "No `ClientSecret`, `ApiToken` or `Google`/`Jira`/`Xray` section in the new file" is prose, not a command.
- plan.md:127-ish: the Step 1 grep `grep -c "sprint-rituals/" … = 4` is fine, but nothing asserts the `Kestrel` URL value.
**Surfaces:** plan.md § Step 1, § Step 3, § Step 4, § Step 6
**Source:** plan Steps 1, 3, 4, 6. **Issue:** as above. **Impact:** the developer either installs PowerShell 7 ad hoc or reports a PASS on a different shell.
**Fix:** add PowerShell 7 (`winget install Microsoft.PowerShell`) to the Step 6 prerequisites and a one-line precondition on Steps 3–4. Define an explicit `-BuildOnly` switch in the Step 3 contract and use it in the acceptance. Replace the prose with `grep -nE "ClientSecret|ApiToken|\"(Google|Jira|Xray)\"" src/Host/appsettings.Production.json` = 0 hits and `grep -n "http://+:8080"` = 1 hit.

### [UNRANKED] L1 — The most likely first-boot failure may leave no readable log
**Anchor:** unranked — no anchor fits; persona grades
**Shape:** observability prerequisite missing
**Consequence:** The first-boot failure the plan expects most would leave no readable log, since container logging is never switched on.
**Evidence:** quoted — plan.md:120 "log stream shows `SQLite journal mode for … : delete`", but Step 4's setting list (plan.md:89-91) has no log configuration. The old runbook enables it explicitly: `<fokus-repo>/docs/deployment-azure.md` § 9 "az webapp log config --docker-container-logging filesystem".
**Surfaces:** plan.md § Step 4, § Step 9; `<fokus-repo>/docs/deployment-azure.md`
**Source:** plan Steps 4 and 9. **Issue:** Step 9 relies on log output, and so does diagnosing J1's first-boot failure, but nothing turns logging on. **Impact:** a failed first boot is diagnosed blind. Reader's suggested level: LOW.
**Fix:** `provision.ps1` sets `--docker-container-logging filesystem`, and the runbook includes `az webapp log tail`.

### [UNRANKED] I1 — The invite link's scheme depends on forwarded headers, and nothing checks it
**Anchor:** unranked — no anchor fits; persona grades
**Shape:** consequence of the proxy fix left unverified
**Consequence:** Invitation links are built from the request scheme, and no check confirms they come out secure after the proxy fix.
**Evidence:** `CreateInvitationEndpoint.cs:39` `$"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/invite/{invitation.Token}"`; plan.md:120 ends with "invite the team" and verifies only the login redirect.
**Surfaces:** `Fokus.API/Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs`; plan.md § Step 9
**Source:** plan Step 9. **Issue:** the proxy fix is checked only through the Google redirect. If forwarded headers were not honoured, invitation links would be `http://` yet the sign-in would still work. **Impact:** the team receives plain-HTTP invitation URLs. Reader's suggested level: LOW.
**Fix:** Step 9 adds "create one invitation and confirm the copied link starts with `https://<old-fokus-app>.azurewebsites.net/invite/`".

### [UNRANKED] M2 — Step 2 names an API the shared library cannot call, and validates after the database exists
**Anchor:** unranked — no anchor fits; persona grades
**Shape:** mechanics under-specified at the code seam
**Consequence:** The journal-switch step names an API the shared library cannot call and validates input after the database was already created.
**Evidence:** `Blocks.EntityFrameworkCore.csproj:10-11` references only `Microsoft.EntityFrameworkCore` and `.Relational`, with no Sqlite package, so `Database.IsSqlite()` (plan.md:67 "only for a SQLite provider") is unavailable. The test name plan.md:74 `JournalMode_InvalidValue_Throws … throws before any SQL runs`, but plan.md:67 sequences the switch *after* `Migrate()`, so SQL has already run. `PRAGMA journal_mode` returns a row, so `ExecuteSqlRaw` and `SqlQueryRaw<string>` (needs a column named `Value`) do not return the effective mode.
**Surfaces:** plan.md § Step 2; `Blocks.EntityFrameworkCore.csproj`; `DbContextExtensions.cs:14-24`
**Source:** plan Step 2. **Issue:** as above. **Impact:** the developer gets stuck on three small choices. Reader's suggested level: LOW.
**Fix (settled here, since this mode owns mechanics):** detect with `context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"`. Read the effective mode via `GetDbConnection()` plus `DbCommand.ExecuteScalar()`. Validate `Sqlite:JournalMode` against the allow-list at the top of `Migrate<T>`, before `EnsureDataDirectory`. J1 item 1 additionally moves the pragma before `Migrate()`. The new test project reuses `Microsoft.EntityFrameworkCore.Sqlite` (already in `Directory.Packages.props`).

## Gap Analysis
- **Operator rights in Step 8:** it assumes the owner can create an Entra app registration and federated credential in the work tenant and assign a role at site scope. The plan's Open Questions say "None", but this is unconfirmed.
- **Verified as safe (checked against code):**
  - `--clean true` clears `/home/site/wwwroot`, not `/home/data`.
  - Published `appsettings.json` is Host's own.
  - `ClientId` is required, but the live app already has `Google__ClientId`.
  - Placeholder Jira values satisfy `ValidateOnStart`, so a missing Jira secret fails at first Jira call, not at startup.
  - The absolute `returnUrl` sent by the SPAs (`LoginView.vue:18`, `retro/stores/auth.ts:23`) makes `Auth:AllowedReturnOrigins` genuinely required in Production, as the plan says.
  - Cookie `SecurePolicy.SameAsRequest` needs the forwarded-proto fix to issue Secure cookies.
- **Edge probe:** a smoke check that polls `/` without following redirects (`-MaximumRedirection 0` throws on 3xx in PowerShell 7 unless `-SkipHttpErrorCheck`) is an executor detail. It is acceptable but should be named in Step 3.
- **Backward impact:** adding `Blocks.EntityFrameworkCore.Tests` to the slnx changes `dotnet test SprintRituals.slnx`. The baseline is green (339 tests), so CI starts green.
- **Calibration note for the learner:** the seed plan ruler has no anchors for deployment, security-posture or data-safety shapes. Eleven of the twelve findings here are UNRANKED. Recommend grading candidates from this record into those plan-kind cells (record against the calibration data per the skill).

## Open Questions
- Is `github.com/ldumit/sprint-rituals` private, and who has write access? This changes S1 from HIGH toward MEDIUM only; it does not remove the fix. Unconfirmed (`gh` not available).
- Does Basic-tier automatic backup cover a Linux app's `/home/data`? Unconfirmed from the Microsoft Learn page (D1).
- Does the live `Database` connection string really equal `/home/data/fokus.db`? Source is the Fokus docs, not a live read (F-A).

Reviewed: docs/specs/F8-AzureDeployment/delivery/plan.md

### Merge — round 1

Coverage: live `<old-fokus-app>` Azure state; GitHub repository settings and visibility

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | SQLite is officially unsupported on Linux App Service storage | governing premise contradicted by vendor doc | The storage decision rests on a premise Microsoft documents as blocked | MS Linux FAQ "Can I use a file based database (like SQLite)…" (verified by architect fetch); plan.md:17; ADR-024 Storage | data-loss | storage premise | ADR-024; plan Steps 1–2 | codex-1, J1 |
| R1-02 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Journal switch runs after EF creates and migrates in WAL | ordering | First boot runs in the mode the plan calls unsafe, and may crash before the switch | plan.md:67,129; DbContextExtensions.cs:20-21; EF SqliteDatabaseCreator | data-loss | storage premise | plan Step 2 | codex-2, J1, M2 |
| R1-03 | codex, sonnet | contested: HIGH / MEDIUM → MEDIUM | seed-0130 (sonnet) | Deploy workflow never produces `artifacts/publish` | contradiction | First deploy run fails before any Azure call | plan.md:81,99-100 | — | workflow | plan Steps 3, 5 | codex-3, P1 |
| R1-04 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Provisioning breaks the live app before the new build exists; no saved old build for rollback | cutover sequencing | Outage of the work app with a rollback that must be rebuilt first | plan.md:89,108,113-114 | — | cutover | plan Steps 4, 6–9 | codex-4, F-B |
| R1-05 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Old DB backup not a consistent snapshot; old path doc-derived; delete unverified | invariant overclaimed | The one rollback copy may be inconsistent or altered | plan.md:114,136; proposal line 38 | data-loss | cutover | plan Steps 4, 7; Must-NOT-Change | codex-5, F-A |
| R1-06 | codex, sonnet | contested: HIGH / MEDIUM → HIGH | unranked — no anchor fits; Codex severity HIGH | First Google sign-in on the public URL becomes Admin | exposure window | A stranger could become permanent Admin | AuthConfiguration.cs:80-84,194; plan.md:120 | security | auth | plan Step 9 | codex-6, S3 |
| R1-07 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Deploy identity available during `npm ci`; `production` environment ungated | least privilege | A poisoned package or unreviewed branch could deploy and read secrets | plan.md:100,117,161 | security | workflow | plan Steps 5, 8 | codex-7, S1 |
| R1-08 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | ADR backup clause has no step, no restore test | clause not delivered | No verified restore point for the new data | decisions.md ADR-024 Storage; plan.md (no hit) | data-loss | backup | plan Steps 4, 6, 9 | codex-8, D1 |
| R1-09 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Acceptance lines check text, name undefined switches, need PowerShell 7 not installed | acceptance not mechanical | A PASS proves less than it claims | plan.md:60-95; `where.exe pwsh` none | — | acceptance | plan Steps 1–6 | codex-9, C1 |
| R1-10 | codex, sonnet | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Smoke test misses redirect_uri, all gRPC paths, invite links, WebSockets, logging | verification gap | Live failures surface only after the team is on it | plan.md:120; CreateInvitationEndpoint.cs:39; old runbook § 9 | — | smoke test | plan Steps 4, 9 | codex-10, I1, L1 |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Reflekt "migrate before switch" gate missing from runbook | clause not carried | Reflekt users could be moved before F9 | plan.md:27,108 | — | scope gate | plan Step 6 | codex-11 |
| R1-12 | sonnet | MEDIUM | unranked — no anchor fits; persona grades | `az` default output echoes secret values | invariant violated by tool | Secrets land in terminal or transcript | plan.md:91 | security | secrets | plan Step 4 | S2 |
| R1-13 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Context overstates the prior-artifact search | wording | Minor misdescription | plan.md:11 | — | context | plan Context | codex-12 |

Merge notes: R1-03 settled MEDIUM — loud, pre-deploy failure, nothing ships (sonnet's anchor reasoning). R1-06 settled HIGH — security class is never downgraded.

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-05)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | waived | HIGH | — | ADR-024 Target + Storage (owner ruled 2026-10-05: zero cost binding, both live apps run this way, few users); plan Context, Scope, Migration Notes |
| R1-02 | fixed-real | — | — | plan old Step 2 removed (no journal-mode switch); Data Model Changes; Testing Strategy |
| R1-03 | fixed-real | — | — | plan Step 2 (`-BuildOnly`/`-SkipBuild` + missing-artifact guard), Step 4 (`build` job produces `deploy.zip`) |
| R1-04 | fixed-real | — | — | plan Step 3 (`-Prepare`/`-Cutover`), Step 6, Step 7, Step 5 Rollback, Decisions |
| R1-05 | fixed-real | — | — | plan Step 3 `-Cutover` (records live path, post-condition), Step 7 (stopped site, -wal/-shm, checksums), Must-NOT-Change |
| R1-06 | fixed-real | — | — | plan Step 7 (access restriction + Admin check + recovery), Step 5 |
| R1-07 | fixed-real | — | — | plan Step 4 (two jobs, SHA pins), Step 6 (`production` limited to `main`), Must-NOT-Change, Decisions |
| R1-08 | waived | MEDIUM | — | owner deferred to backlog F10 (2026-10-05); ADR-024 Storage; plan Scope, Step 5 |
| R1-09 | fixed-real | — | — | plan Steps 1–5 Accept (JSON value checks, `HEAD` diff, explicit switches, 5.1 compatibility) |
| R1-10 | fixed-real | — | — | plan Step 3 (logging), Step 8 (redirect_uri, Secure cookie, all gRPC paths, WebSocket 101, invite redemption, restart) |
| R1-11 | fixed-real | — | — | plan Step 5 (Reflekt gate) |
| R1-12 | fixed-real | — | — | plan Step 3 (secret-safe output + accept grep) |
| R1-13 | fixed-real | — | — | plan Context (prior artifacts sentence) |

Reconciled: ADR-024 (Target, Storage, Non-secret config) ↔ plan Context/Scope/Step 1/Step 3; Must-NOT-Change ↔ Steps 1, 3, 4; Decisions ↔ Steps 1–4; runbook accept list (Step 5) ↔ Steps 3, 6, 7; backlog F10 row ↔ ADR-024 Storage. Step numbers shifted (old 3–9 → new 2–8) — every in-plan cross-reference rewritten with them.

## Round 2 — 2026-10-05 — plan — primary (codex)

Header note: a full re-read of the plan against the repo, because trigger (a) fired on round 1's Coverage line (live Azure state and GitHub settings were unopened, and neither can be opened from the repo). Primary only (the second reader joins round 1 only). Codex verdict NO-GO; its text is in `review-critic-codex.md` § round 2. Round-1 closure per Codex: closed R1-02, R1-05, R1-07, R1-11, R1-12, R1-13; partial R1-04, R1-06, R1-09, R1-10; not closed R1-03.

### Merge — round 2

Coverage: live `<old-fokus-app>` Azure state; GitHub repository settings and visibility

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `-SkipBuild` needs an unpacked publish folder the deploy job never has | contradiction | First workflow deploy fails | plan Step 2 modes; Step 4 artifact | — | fold | plan Steps 2, 4 | codex-R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `-Prepare` changes settings, which restarts the live app | broken guarantee | Interruption outside the cutover window | plan Step 3 -Prepare; Step 6 title | — | fold | plan Steps 3, 6 | codex-R2-02 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Logs at Information level cannot show the database paths | verification gap | Smoke line unprovable | plan Step 8 | — | fold | plan Step 8 | codex-R2-03 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `git diff HEAD` does not pin unchanged dev config after commit | acceptance not mechanical | Invariant overstated | plan Step 1; Must-NOT-Change | — | fold | plan Step 1, Must-NOT-Change | codex-R2-04 |
| R2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Recovery from a foreign Admin leaves the foreign company domain in Fokus | incomplete recovery | Legitimate invitations fail | SeedCompanyDomainOnFirstAdministratorRegisteredConsumer.cs:21 | security | fold | plan Step 7 | codex-R2-05 |
| R2-06 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | The "sign in within a minute" fallback reopens the first-admin race | broken guarantee | A stranger could become Admin | plan Step 7 fallback; Free tier supports access restrictions | security | fold | plan Step 7 | codex-R2-06 |
| R2-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Deploy smoke check conflicts with a stopped site and the IP restriction | sequencing | Cutover deploy fails or hangs | plan Step 2 smoke; Step 7 order | — | fold | plan Steps 2, 7 | codex-R2-07 |
| R2-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | A retried `-Cutover` overwrites the rollback snapshot | broken guarantee | Rollback metadata lost | plan Step 3 -Cutover | — | fold | plan Step 3 | codex-R2-08 |
| R2-09 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The 5.1 compatibility claim is not exercised by any acceptance | acceptance not mechanical | Laptop deploy may break on 5.1 | plan Step 2 accept | — | fold | plan Steps 2, 4 | codex-R2-09 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-05)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | plan Step 2 (`-SkipBuild` checks `Host.dll` inside the zip) |
| R2-02 | fixed-real | — | — | plan Step 3 (`-Prepare` read-only; all changes moved into `-Cutover`), Step 6 |
| R2-03 | fixed-real | — | — | plan Step 8 (Kudu `ls` of the four files + old-DB checksum unchanged) |
| R2-04 | fixed-real | — | — | plan Step 1 accept + Must-NOT-Change (baseline commit `4affe6a`) |
| R2-05 | fixed-real | — | — | plan Step 7 recovery (delete all four databases) |
| R2-06 | fixed-real | — | — | plan Step 7 step 1 (restriction mandatory, 403 confirmed from outside, else stop); Must-NOT-Change row |
| R2-07 | fixed-real | — | — | plan Step 2 (`--track-status false`, `-SmokeOnly`), Step 7 (no site stop; cutover deploy from the laptop inside the restriction), Decisions |
| R2-08 | fixed-real | — | — | plan Step 3 (write-once snapshot, refuse if already `Host.dll`, idempotent re-run) + accept |
| R2-09 | fixed-real | — | — | plan Step 2 (`curl.exe` probe, `-SmokeOnly` accepts on 5.1 and pwsh), Step 4 (ci.yml runs them) |

Reconciled: Step 3 phases ↔ Steps 6, 7 and Decisions; Step 2 modes ↔ Step 4 jobs and ci.yml; Step 7 order ↔ Step 8 (runs inside the restriction) and the Step 5 runbook list; Must-NOT-Change ↔ Steps 1, 7. No round-2 finding was CRITICAL, so no round 3 runs.
