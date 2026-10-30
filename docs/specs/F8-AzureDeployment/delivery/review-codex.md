# F8-AzureDeployment — Codex code-review record

Persisted by the standalone architect (session b1593ba1) from Codex job task-mvuqcl53-n4k81h (label `F8-AzureDeployment code-review round 1`, no --effort: repo sets no codex.effort). Companion trailer lines cut.

## Verdict: NO-GO

1. **HIGH — [.github/workflows/ci.yml:39](.github/workflows/ci.yml:39)** — The clean runner starts the Host without Google configuration. Both checked-in Google credentials are empty. Tracing ASP.NET authentication shows that `/` initializes the Google handler, whose validation rejects those values; the positive smoke check therefore cannot reach the expected redirect. **Fix:** supply explicit, non-secret dummy `Google__ClientId` and `Google__ClientSecret` values for the CI Host, then verify both PowerShell 7 smoke runs. This follows from the [authentication middleware](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Security/Authentication/Core/src/AuthenticationMiddleware.cs) and [OAuth validation](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Security/Authentication/OAuth/src/OAuthOptions.cs).

2. **MEDIUM — [deploy/provision.ps1:166](deploy/provision.ps1:166)** — Snapshot reuse checks only file existence. After changing the configurable target, another app’s snapshot is accepted as a resumed cutover; the script then changes startup and removes `Database` without saving that target’s previous configuration. **Fix:** scope snapshots by subscription/resource group/app, or validate their recorded target and required fields before allowing any writes. Preserve the existing snapshot on mismatch.

3. **LOW — [deploy/deploy.ps1:155](deploy/deploy.ps1:155)** — Each probe receives a fixed 20-second timeout, and successful responses bypass the elapsed-time check. Consequently, `-TimeoutSeconds 10` can exceed its deadline and still report success. An in-memory probe reproduced success after the configured deadline. **Fix:** calculate the remaining time before each request, cap curl’s timeout accordingly, and check the deadline before accepting success.

4. **LOW, pre-existing — [CLAUDE.md:77](CLAUDE.md:77)** — The database table and “Both databases” sentence describe two databases, while Host migrates four. **Fix:** add Reflekt and Poker with migration positions three and four, and update the sentence.

Every Carry-Over Findings row was assessed:

| Carry-over row | Assessment |
|---|---|
| Step 2 command checks not run by developer | **Confirmed historically; discharged for Windows PowerShell 5.1** by the main-session results at implementation.md:34. The existing zip independently passes the required-entry and forward-slash checks. PowerShell 7 remains unverified and is blocked by finding 1. |
| Provisioning reads assume `az` output shapes | **Confirmed as a remaining LOW verification gap — [provision.ps1:85](deploy/provision.ps1:85).** The installed CLI’s JMESPath/TSV formatter produces the expected modeled layouts, but live resource values remain unverified. **Fix:** run operator Step 6 `-Prepare` and record the guard, plan/apps, startup and database-path results. |
| CLAUDE.md lists only two databases | **Confirmed, pre-existing.** Finding 4 gives the location and fix. |

Acceptance bullets, numbered in their order within each step:

| Step | Assessment |
|---|---|
| 1 | **A1–A5 pass:** JSON structure, exact paths and unchanged development configuration verified; publish evidence supported by the record and existing zip. |
| 2 | **A1–A3 and A5 pass. A4 partially met:** recorded Windows checks pass; the PowerShell 7 pair remains open. |
| 3 | **A1–A6 pass** as static/dry-run requirements. Snapshot preservation passes mechanically, but target validation is missing as described in finding 2. |
| 4 | **A1–A5 pass:** triggers, permissions, build/deploy separation, SHA pins and target exclusions match. A6 correctly reserves the first green run for the operator; successful CI operation is currently blocked by finding 1. |
| 5 | **A1–A2 pass:** every required runbook term and both ADR sentences are present. |

Normal Azure output is appropriately constrained: writes use `-o none`, configuration reads are captured and filtered, and the `Database` removal has a checked post-condition. OIDC permission placement and site-scoped role assignment match the plan.

Verification remained read-only. No files were changed.
