# F8-AzureDeployment — Codex critic record

## Verdict: NO-GO — round 1 (plan review, 2026-10-05)

Persisted by the standalone architect from the Codex job output (job task-muuulod9-emywkk; the helper found no machine-readable verdict line, the text opens with NO-GO).

**NO-GO — Round 1.** The plan has deployment blockers, an unsafe cutover sequence, and acceptance gaps around storage, authentication, and production access. This was a read-only review; no files were modified and no Azure actions were performed.

ADR-024 coverage is as follows:

| ADR-024 clause | Delivering steps | Assessment |
|---|---|---|
| Existing site, B1, one instance, AlwaysOn, HTTPS-only | 4, 8 | Specified; effective Azure state is not checked |
| Single-instance constraint | 4, 6 | Covered in configuration and documentation |
| Four persistent databases, production `DELETE`, local WAL | 1, 2 | Paths covered; initialization order and storage safety unresolved |
| Hourly automatic backups; escalation on lock errors | 4 implicitly enables backup eligibility | Backup verification and recovery procedure missing |
| Production configuration in code; secrets in App Service; forwarded scheme | 1, 4, 6, 8 | Design covered; acceptance incomplete |
| One deploy script, CI, manual deployment, OIDC | 3, 5, 6, 8 | Workflow cannot currently produce its required deployment input |
| Fresh Fokus data; Reflekt migration before switching users | 1, 7, 9; F9 deferred | Fokus covered; Reflekt switch prerequisite lacks an explicit gate |

The repository assertions in Context lines 14–21 match the inspected code: endpoint URLs and five gRPC clients, development-only return origins, Google callback path, absence of `UseForwardedHeaders`, migration order, root redirect, frontend build outputs and ignore rule, lockfile, Host assembly name, absence of Host integration tests and BuildingBlocks test projects, absence of `.github`, and the remote URL. All four local database headers were read directly and contain bytes `2 2`. Azure resource state remains evidence from the proposal, rather than independently verified tenant state.

1. **HIGH — `DELETE` does not establish that SQLite is safe on this mount.**  
   **File:** [plan.md:17](docs/specs/F8-AzureDeployment/delivery/plan.md:17), Steps 2 and 9; related [ADR-024](docs/architecture/decisions.md:131).  
   **Issue:** The plan addresses WAL’s network-filesystem restriction but overlooks Microsoft’s separate statement that Linux App Service’s mounted filesystem blocks SQLite because exclusive locks cannot be acquired. Successful startup and a short functional smoke test do not resolve that constraint. This is a conflict in the ratified storage premise, not evidence that corruption has already occurred. [Microsoft’s Linux App Service FAQ](https://learn.microsoft.com/en-us/troubleshoot/azure/app-service/faqs-app-service-linux-new)  
   **Suggested fix:** Escalate the contradiction to ADR-024’s owner before deployment approval. Establish a supported storage arrangement, or amend the ADR. Any proposed exception needs explicit evidence about locking, concurrent writes, recovery, and integrity on the actual mount.

2. **HIGH — The journal-mode switch occurs after the unsafe initialization.**  
   **File:** [plan.md:67](docs/specs/F8-AzureDeployment/delivery/plan.md:67), also line 129.  
   **Issue:** EF Core’s SQLite creator sets WAL when creating a database. `Database.Migrate()` therefore creates and migrates fresh production databases in WAL before the proposed switch executes. Startup can fail on CIFS before reaching the safeguard. Validation after migration also contradicts the acceptance requirement that an invalid value throws “before any SQL runs.” [EF Core 10 SQLite creation source](https://github.com/dotnet/efcore/blob/v10.0.0/src/EFCore.Sqlite.Core/Storage/Internal/SqliteDatabaseCreator.cs)  
   **Suggested fix:** Validate configuration before migration and initialize production files in `DELETE` without invoking EF’s WAL-setting creation path. Test the SQL sequence for a fresh database, restart persistence, and failure when the requested mode cannot be applied. SQLite can return the previous mode when a change fails, so the missing mismatch test matters. [SQLite journal-mode documentation](https://www.sqlite.org/pragma.html#pragma_journal_mode)

3. **HIGH — The manual workflow never creates `artifacts/publish`.**  
   **File:** [plan.md:100](docs/specs/F8-AzureDeployment/delivery/plan.md:100).  
   **Issue:** “Build + test as CI” performs `dotnet build`, `dotnet test`, and frontend builds. None produces the publish directory required by `deploy.ps1 -SkipBuild`. A fresh GitHub runner therefore has nothing to package.  
   **Suggested fix:** After building the SPAs, invoke the script’s build-only path to create the publish output, run the required tests, and deploy that same output with `-SkipBuild`. Alternatively, specify an explicit publish step. Acceptance must exercise this sequence on a clean checkout.

4. **HIGH — Provisioning breaks the existing app before deployment is ready.**  
   **File:** [plan.md:89](docs/specs/F8-AzureDeployment/delivery/plan.md:89), Steps 8–9 and rollback at line 108.  
   **Issue:** Step 8 changes startup to `dotnet Host.dll` while the deployed application still contains `Fokus.API.dll`. It also removes the old app’s `Database` connection string. These changes precede OIDC setup and the first deployment, creating an uncontrolled outage. Rollback lacks a coordinated stop/configuration/deployment sequence and does not require preserving the old deployable package before cleaning it away.  
   **Suggested fix:** Complete build and identity setup first. Define a maintenance cutover that stops the site, applies configuration and the prepared package, then starts and verifies it. Preserve and validate the old package beforehand, and document the corresponding stopped-site rollback sequence.

5. **HIGH — Downloading the live `.db` alone is not a reliable rollback backup.**  
   **File:** [plan.md:114](docs/specs/F8-AzureDeployment/delivery/plan.md:114).  
   **Issue:** The plan downloads a database from a running application without establishing a consistent snapshot. Active transactions can produce an inconsistent copy; committed WAL content may reside outside the main file. There is no integrity or restore acceptance. [SQLite’s backup and corruption guidance](https://www.sqlite.org/howtocorrupt.html#backup_or_restore_while_a_transaction_is_active)  
   **Suggested fix:** Specify a consistent SQLite snapshot procedure, or quiesce the old application and preserve the required database/journal files together. Validate a copy offline with integrity checks and the old application, and record the backup location and checksum before cutover.

6. **HIGH — Public deployment exposes the first-admin bootstrap race.**  
   **File:** [plan.md:120](docs/specs/F8-AzureDeployment/delivery/plan.md:120); evidence: [AuthConfiguration.cs:80](src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:80).  
   **Issue:** With a fresh Identity database, the first successful Google login creates an Admin. The code does not restrict that bootstrap to the owner. “Owner signs in first” is an instruction, not an enforced safeguard; another visitor to the existing public URL can win the race.  
   **Suggested fix:** Restrict application access to the owner during bootstrap, verify the resulting Admin and company domain, then open access. Add explicit acceptance for that restriction and its removal.

7. **HIGH — The OIDC environment subject does not restrict deployment to reviewed code.**  
   **File:** [plan.md:100](docs/specs/F8-AzureDeployment/delivery/plan.md:100), OIDC setup at line 108.  
   **Issue:** The federated subject identifies `production`, but the plan does not configure that environment’s allowed branches/tags. A manual dispatch can select another branch, and other workflows referencing the environment can obtain the same identity. `workflow_dispatch` alone does not establish the production trust boundary. [GitHub’s Azure OIDC guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-azure)  
   **Suggested fix:** Restrict `production` to the designated protected deployment branch. Give `id-token: write` only to the deployment job, keep build/test jobs without production credentials, pin privileged actions to reviewed commits, and verify the federated subject, issuer, audience, and site-only role scope.

8. **MEDIUM — Automatic backup coverage has no acceptance or recovery procedure.**  
   **File:** [plan.md:108](docs/specs/F8-AzureDeployment/delivery/plan.md:108).  
   **Issue:** B1 makes the app eligible for hourly automatic backups without additional configuration, and normal Linux `/home` content is included. However, no step verifies snapshots exist or demonstrates recovery of the four databases. File backup coverage also does not itself establish SQLite transaction consistency. [App Service backup documentation](https://learn.microsoft.com/en-us/azure/app-service/manage-backup)  
   **Suggested fix:** Add snapshot verification and a restore procedure that checks all four databases on a separate target. Document backup limitations and the operational response to lock errors.

9. **MEDIUM — Several acceptance lines check text rather than the stated contract.**  
   **File:** [plan.md:60](docs/specs/F8-AzureDeployment/delivery/plan.md:60), acceptance blocks for Steps 1–6.  
   **Issue:** Four matching lines do not prove the four connection-string keys or values, port, origins, or journal mode. An empty `git diff` does not detect changes already committed. The zip listing does not prove failure propagation. Provisioning greps and dry-run output do not prove successful deletion of the overriding connection string, effective flags, or idempotence. Trigger greps accept forms such as `on: [workflow_dispatch, push]` and omit other automatic events. `secrets.` greps cannot detect literal credentials, and runbook keywords do not validate its procedures.  
   **Suggested fix:** Parse JSON and YAML and assert exact values and trigger sets; compare local configuration against a pinned baseline; check every archive entry; exercise native-command failures and dry-run behaviour; verify redacted Azure state after provisioning; inspect tracked content for credentials; and require executable backup/rollback instructions. In particular, verify the overriding `Database` setting is absent before Host starts.

10. **MEDIUM — The live smoke test does not prove all its claimed behaviours.**  
    **File:** [plan.md:120](docs/specs/F8-AzureDeployment/delivery/plan.md:120).  
    **Issue:** Landing on HTTPS does not prove that the outgoing Google `redirect_uri` used HTTPS. The retro picker exercises `SprintCatalog`, not all five gRPC services. Fokus sync can succeed while Person reconciliation fails because that exception is swallowed. Creating invitations does not exercise their redemption. Real-time votes can use SignalR fallback transports without proving WebSockets.  
    **Suggested fix:** Inspect the OAuth challenge’s exact `redirect_uri` and Secure cookies; verify Person reconciliation, retro analytics, and invitation redemption; observe a successful WebSocket upgrade; and restart the app to verify persisted data and effective journal modes. Record concrete results for each check.

11. **MEDIUM — The Reflekt migration prerequisite is not carried into the runbook.**  
    **File:** [plan.md:27](docs/specs/F8-AzureDeployment/delivery/plan.md:27), runbook requirements at line 108.  
    **Issue:** Deferring migration to F9 is correct, but ADR-024 also requires migration **before existing Reflekt users switch**. The runbook has no explicit gate enforcing that condition, while Step 9 ends with inviting the team.  
    **Suggested fix:** State explicitly that existing Reflekt users remain on the personal-account app until F9 migration acceptance is complete. Distinguish F8 smoke-test users from the existing-user transition.

12. **LOW — Context overstates the document-search result.**  
    **File:** [plan.md:11](docs/specs/F8-AzureDeployment/delivery/plan.md:11).  
    **Issue:** The documented `deploy|azure` search returns multiple proposals and historical specs/plans. I found no competing suite Azure deployment plan, but “only the proposal above” is not an accurate description of the search result.  
    **Suggested fix:** Say that the search found historical deployment references but no other plan implementing this deployment.

The platform assumptions that are sound should be retained: explicit Kestrel endpoints override lower-priority port configuration; the inherited HTTP/2-only listener on `localhost:5150` is appropriate for in-container h2c; and the forwarded-headers environment flag is an officially documented way to restore the scheme without adding middleware code. The actual platform port and outgoing OAuth redirect still need live verification. [Kestrel endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0), [gRPC protocol requirements](https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0), [forwarded scheme configuration](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0#forward-the-scheme-for-linux-and-non-iis-reverse-proxies)

Deleting the existing App Service connection-string override is necessary because it takes precedence over JSON. ZIP deployment with `--clean true` cleans its deployment target, normally `/home/site/wwwroot`; it does not inherently delete `/home/data/fokus.db`. The site-scoped Website Contributor role and non-secret GitHub identifiers are sensible choices. [Configuration precedence](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore#access-environment-variables), [ZIP deployment behaviour](https://learn.microsoft.com/en-us/azure/app-service/deploy-zip), [Website Contributor permissions](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/web-and-mobile#website-contributor)

Codex session ID: 01a10aad-22dc-7580-a312-6d36f7e39f09
Resume in Codex: codex resume 01a10aad-22dc-7580-a312-6d36f7e39f09


## Verdict: NO-GO — round 2 (plan review, 2026-10-05)

Persisted by the standalone architect from Codex job task-muux3viy-2akzfo.

1. **Round-1 closure**

Line references are to [plan.md](docs/specs/F8-AzureDeployment/delivery/plan.md).

| Finding | Status | Plan lines and assessment |
|---|---|---|
| R1-02 | closed | L48, L166: journal-mode modification removed; consistent with amended ADR-024. |
| R1-03 | not closed | L68, L95–96: workflow transfers only `deploy.zip`, but `-SkipBuild` also requires unpacked `artifacts/publish/Host.dll`. |
| R1-04 | partial | L81–82, L114, L133–142: phase split and saved binaries address the original outage; preparation restarts and deployment/smoke ordering remain problematic. |
| R1-05 | closed | L82, L135–137: live path recorded, stopped-site snapshot includes sidecars, checksums recorded, deletion verified before deployment. |
| R1-06 | partial | L134, L141–144: restriction and verification added, but an unenforced timing fallback remains; recovery leaves the company domain behind. |
| R1-07 | closed | L92–102, L124–127: separate build/deploy jobs, restricted production branch, scoped identity, pinned actions. |
| R1-09 | partial | L58–76, L85–102: checks improved, but moving-`HEAD` comparison and incomplete PowerShell deployment acceptance remain. |
| R1-10 | partial | L81, L148–156: expanded smoke coverage addresses the functional gaps; migration-path evidence requires logging that is not enabled. |
| R1-11 | closed | L113: existing Reflekt users explicitly remain on the old app until F9. |
| R1-12 | closed | L83, L88: settings/connection-string output suppression and restricted queries specified. |
| R1-13 | closed | L11: historical references acknowledged without claiming the proposal was the only search result. |

2. **Contradictions with the plan, ADR-024, or repository**

The revised hosting/storage decisions agree with amended ADR-024. The following MEDIUM+ contradictions remain:

- **R2-01 — MEDIUM — [plan.md:68](docs/specs/F8-AzureDeployment/delivery/plan.md:68): Artifact handoff still cannot satisfy `-SkipBuild`.** L95 uploads only `artifacts/deploy.zip`; L96 downloads that artifact onto another runner. Nothing creates the separately required `artifacts/publish/Host.dll`, so deployment fails before Azure calls. **Suggested fix:** Validate `Host.dll` inside the zip, or explicitly transfer/extract the publish directory. Add acceptance using the downloaded artifact on a clean runner.

- **R2-02 — MEDIUM — [plan.md:81](docs/specs/F8-AzureDeployment/delivery/plan.md:81): Preparation contradicts “no impact on the live app” at L121.** Adding/changing App Service settings triggers a restart; enabling HTTP/2 also restarts the application. On F1 this introduces interruption outside the declared cutover window. **Suggested fix:** Move these mutations into the stopped-site cutover, or explicitly schedule and verify the preparation restart. [App settings behavior](https://learn.microsoft.com/en-us/azure/app-service/configure-common#configure-app-settings), [HTTP-version behavior](https://learn.microsoft.com/en-us/azure/app-service/configure-grpc#configure-the-http-version-and-http-20-proxy).

- **R2-03 — MEDIUM — [plan.md:150](docs/specs/F8-AzureDeployment/delivery/plan.md:150): The specified logs cannot establish the database paths.** Host logging defaults to `Information`; `Migrate<T>()` adds no path logging. EF’s migration message containing the database/data-source is `Debug`, while its `Information` message identifies only the migration. Filesystem logging alone does not expose the required evidence. **Suggested fix:** Enable the relevant EF migration category at `Debug` before first boot, or require direct verification of the four files and their migration histories. [EF logging definitions](https://github.com/dotnet/efcore/blob/v10.0.0/src/EFCore.Relational/Properties/RelationalStrings.resx).

- **R2-04 — MEDIUM — [plan.md:62](docs/specs/F8-AzureDeployment/delivery/plan.md:62): Moving `HEAD` does not pin unchanged development configuration.** After implementation is committed, `git diff HEAD` is empty even if either existing configuration file changed. L172 therefore overstates the invariant’s protection. **Suggested fix:** Compare against a recorded pre-F8 commit or fixed baseline hashes.

- **R2-05 — MEDIUM — [plan.md:144](docs/specs/F8-AzureDeployment/delivery/plan.md:144): Deleting Identity alone does not recover a foreign bootstrap.** The first administrator can seed `CompanyDomain` in Fokus. Its [consumer refuses to overwrite an existing domain](src/Services/Fokus/Fokus.API/Features/Settings/Consumers/SeedCompanyDomainOnFirstAdministratorRegisteredConsumer.cs:21), so the replacement owner’s sign-in leaves the foreign domain in place and legitimate invitations can fail. **Suggested fix:** Define recovery for all bootstrap-affected state, including Fokus’s domain, then verify the recovered Admin and domain before opening access.

3. **Broken guarantees or new MEDIUM+ risks**

- **R2-06 — HIGH — [plan.md:144](docs/specs/F8-AzureDeployment/delivery/plan.md:144): “Sign in within a minute” reopens the first-admin race.** Any successful Google login during that minute can become Admin. Free supports IP access restrictions—Microsoft lists 512 rules per app—so tier refusal is not a valid reason to substitute timing for enforcement. **Suggested fix:** Require verified owner-only access before starting the fresh Host; abort bootstrap if the restriction fails, and verify outside access is denied. [Free-tier access restrictions](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits).

- **R2-07 — MEDIUM — [plan.md:139](docs/specs/F8-AzureDeployment/delivery/plan.md:139): Deployment’s mandatory smoke check conflicts with cutover.** L70 requires a successful public redirect before the deploy script returns, but Step 7 starts the stopped site afterward. The GitHub runner also falls outside the owner-only IP restriction, so its probe receives 403 even when the application runs. Linux deployment startup tracking adds another incompatible wait. **Suggested fix:** Specify a cutover upload mode without application startup tracking or public smoke polling; start the site and verify it from the allowed operator before removing restrictions. [CLI deployment options](https://learn.microsoft.com/en-us/cli/azure/webapp?view=azure-cli-latest#az-webapp-deploy).

- **R2-08 — MEDIUM — [plan.md:82](docs/specs/F8-AzureDeployment/delivery/plan.md:82): Retrying cutover can destroy rollback metadata.** A repeated `-Cutover` records the now-current `dotnet Host.dll` startup command and absent `Database` setting. No write-once/reuse rule protects the original metadata required by L114. **Suggested fix:** Save and validate the original snapshot before mutation, preserve it across retries, and explicitly handle an already-completed or partially completed cutover.

- **R2-09 — MEDIUM — [plan.md:75](docs/specs/F8-AzureDeployment/delivery/plan.md:75): Acceptance does not cover the PowerShell 5.1 deployment claim.** Build-only and provisioning dry runs never exercise the required redirect probe or native-command failures. PowerShell 5.1 lacks `-SkipHttpErrorCheck`; updated `Invoke-WebRequest` also prompts unless `-UseBasicParsing` is used. **Suggested fix:** Specify a compatible, noninteractive HTTP probe and exercise expected redirects, transient failures, timeout, and native failure propagation under both shells. The proposed compression API itself worked in an in-memory 5.1 check. [PowerShell 5.1 behavior](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/invoke-webrequest?view=powershell-5.1).

No Free-tier availability blocker was found for filesystem container logging, ZIP deployment, or HTTP/2. [Logging and ZIP on F1](https://learn.microsoft.com/en-us/azure/app-service/quickstart-nodejs), [container logging](https://learn.microsoft.com/en-us/azure/app-service/configure-language-python#access-diagnostic-logs), [HTTP/2 availability](https://azure.microsoft.com/en-us/blog/announcing-http-2-support-in-azure-app-service/). R1-01 and R1-08 remain settled owner decisions. No files were modified.

Codex session ID: 01a10aed-4524-77c0-8d7f-d6ae24a05ff1
Resume in Codex: codex resume 01a10aed-4524-77c0-8d7f-d6ae24a05ff1

