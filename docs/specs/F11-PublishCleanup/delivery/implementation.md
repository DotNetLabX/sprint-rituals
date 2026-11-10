# F11 — Publish Cleanup — Implementation

**Model:** claude-opus-5-5

## Step 1 — Remove build output and tool state; fix `.gitignore`
**Files:** `src/Services/Fokus/Fokus.API/deploy.zip` — deleted (old publish archive, build output); `src/Services/Fokus/Fokus.API/wwwroot/` — deleted (old client build output, referenced by no project); `client/apps/retro/.omc/` and `docs/specs/fokus/adhoc-SuiteScaffoldingV2/delivery/.omc/` — deleted (an older agent tool's state, held a machine path); `.gitignore` — appended the six unanchored rules `.omc/`, `deploy.zip`, `src/Services/*/*.API/wwwroot/`, `.claude/.worktree-target`, `.claude/approval-scope/`, `.claude/pipeline-state/` under a new comment heading; existing rules unchanged
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** None. Acceptance: the file count prints 0 (plan time 50); the `--no-index` ignore proof prints nothing for all five probe paths. Step run attempt 1 (build only): pass.

## Step 2 — Remove the private documents
**Files:** the three items under `docs/` whose names carry the employer-internal harness's abbreviation — one proposal under `docs/proposals/`, one research note under `docs/audit/research-raw/`, one ad-hoc feature-record folder under `docs/specs/` — deleted; `docs/product/suite-strategy.md` — deleted (business strategy); `docs/proposals/suite-integration-architecture.md` — truncated after line 230 (the end of § 13 Migration), removing the two blank lines and the pasted retro report from its `# Sprint 28` heading to the end of the file (41 lines removed, none added; line endings unchanged)
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** technical: the proposal's § 13 sentence that mentions "the suite strategy document" stays — proposals are not live docs and the plan names no edit there; its private references are step 3's
**Deviations:** None. Acceptance: the file-name search prints 0 (plan time 3); the strategy file is absent; the name/report search prints nothing (plan time 4).
**Test problem:** step run — record `skipped` (nothing to run for a docs-only change) — not fixed — nothing to fix; the complete run at close covers it

## Step 3 — Replace employer, people, cloud and private-project references
**Files:** the site list is the output of the six acceptance searches run after step 2 (183 hit lines in 18 files); each file below had its hits replaced by a scratchpad script (literal replacements, one fixed mapping, line endings preserved) and nothing else:
`deploy/deploy-config.example.ps1` — both values now `<app-name>` and `<resource-group>`;
`docs/deployment-azure.md` — app, old app, resource groups, plans, deploy identity → the plan's placeholders; tenant and subscription display names → "the work tenant", "the subscription" (the `az account set` command now takes `"<subscription>"`); the two repository lines → `<github-owner>/<repo>`; every command still reads as runnable with the placeholders filled in;
`docs/architecture/decisions.md` — ADR-024's target and data-carry-over lines: cloud names → placeholders;
`docs/product/fokus/v2.md` — Jira tenant → `acme.atlassian.net`, project key → `ABC`, five ticket keys → `ABC-101`…`ABC-105` (one fixed key each, same everywhere), the release name → "Release A", two feature-area names → "feature area B", "feature area C", two colleagues → "QA engineer A", "Developer B";
`docs/specs/fokus/F25-XrayIntegration/definition/spec.md`, `docs/specs/fokus/F25-XrayIntegration/delivery/plan.md`, `docs/specs/fokus/F35-EpicViewEnhancements/definition/help.page.md` — ticket keys → the same `ABC-1xx` keys;
`docs/plugin-feedback/nexus-1.16.1-2026-06-21.md` — project key example → `ABC`;
`docs/proposals/azure-deployment-handoff.md`, `docs/specs/F8-AzureDeployment/delivery/{plan,implementation,review-critic,review-second,summary}.md` — cloud names, tenant and subscription → placeholders and descriptions;
`docs/specs/fokus/adhoc-PipelineCraftImprovements/delivery/plan.md` — the employer's name → "the employer";
`docs/proposals/br-coverage-loop-harness.md`, `docs/audit/golden-set.md`, `docs/conventions/project-rules.md` — the private harness's abbreviation and repo name → "an employer-internal harness" / "the harness"; the links to the evaluation step 2 deleted → "the evaluation of an employer-internal harness (a private document, not in this repo)"
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** technical: ticket keys numbered in order of first appearance in the tree (`ABC-101` to `ABC-105`); the old Reflekt app's host became `<old-reflekt-app>.azurewebsites.net` (the plan names `<old-reflekt-app>`); the region name stays (not private). technical: in `docs/conventions/project-rules.md` (a live doc) the example naming the harness evaluation by its abbreviation became "employer-internal harness evaluation" — the evaluation itself is gone since step 2; step 5 owns any further rewording of live docs
**Deviations:** None. Plan-time counts were taken before step 2; after step 2 the searches started at 60 / 18 / 3 / 85 / 15 / 2 hit lines. Acceptance: all six searches print nothing.
**Test problem:** step run — record `skipped` (nothing to run for a docs and example-config change) — not fixed — nothing to fix; the complete run at close covers it

## Step 4 — Replace machine paths
**Files:** `CLAUDE.md` — hand edit: the Reference Projects table keeps its three rows; the path column became a project column (Fokus (original), Reflekt (original), dotnet-microservices), and one sentence says these are separate repositories whose location is machine-specific; `src/Services/Identity/CLAUDE.md` — the two pattern paths now `<reference-repo>/src/Services/Auth/` and `<fokus-repo>/src/Services/Fokus/Fokus.API/Auth/`; `docs/architecture/decisions.md` (ADR-002 line, also reworded "derive the pattern from this repo" → "from it" so it still means the reference codebase), `docs/architecture/index.md`, `docs/backlog.md` (5 lines; the coordinator's F11 row untouched), `docs/kb/golden/{BugRatioAnalyzer,CycleTimeAnalyzer}.md` — machine paths replaced; plus 87 files under `docs/audit/`, `docs/plugin-feedback/`, `docs/proposals/` and `docs/specs/` (feature records) — machine paths replaced by the same rules, no other line touched. 95 files in all; the full list is the working-tree diff of the hit files of the acceptance search (582 hit lines after step 3).
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** technical: a path *below* another of the owner's repos keeps its repo-relative tail behind a named placeholder — `<fokus-repo>/…`, `<reflekt-repo>/…`, `<reference-repo>/…` (the reference codebase), `<plugin-repo>/…` (the plugin source repo), `<private-repo>/…` — instead of the bare project name, so records that cite a file (`SOURCE:` lines in the golden mining notes, "copied from" lines) still say which file; a bare repo root became the project's name in prose ("the original Fokus project", "the reference codebase", "the plugin source repo", "a private repo"). The plan's words are "that project's name with no path"; I read "no path" as no machine path — the reviewer may rule the stricter reading. technical: a path inside this repo or the worktree became repo-relative; a markdown link target became root-relative (`/docs/…`) so it still resolves; the tree root in an old scaffold plan's code block became `sprint-rituals/`; a home folder became `<home>`, a temp folder `<temp>`, the session-transcript path "the session transcript"; in commands a bare root became a placeholder (`git -C "<repo>"`, `cd <repo> &&`, `cd "<fokus-repo>"`, `git -C "<plugin-repo>"`), never prose. technical: three elided examples the search still matched (a generic `git -C` repo path, "4 candidates on" a drive root, a POSIX-style elided path) became `<repo>`, "the owner's machine" and `/d/...`.
**Ruling (architect, relayed by the coordinator, 2026-11-10):** settled — "no path" means no machine path; keeping the file part behind a named placeholder is correct, since the placeholders point only into the owner's own repos and nothing is dropped.
**Deviations:** None. A follow-up pass removed doubled wording the bare-root replacement produced where the text already said "reference project" or "plugin source repo" before the path (8 lines). Plan-time count 599; after step 3 the search started at 582 lines. Acceptance: both searches print nothing. Scripts (scratchpad only): survey, replacement and cleanup, written with the Write tool.
**Test problem:** step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it

## Step 5 — Fix references in live docs
**Files:** `docs/product/index.md`, `docs/product/reflekt/v1.md`, `docs/product/reflekt/v2.md`, `docs/architecture/reflekt/v1.md`, `docs/architecture/fokus/v1.md`, `docs/architecture/index.md`, `docs/architecture/decisions.md`, `docs/conventions/project-rules.md`, `docs/conventions/kb-entry-schema.md`, `docs/backlog.md` — the edits below and nothing else (scratchpad script with an exact hit count per rule; all counts matched).

Mentions of documents step 2 deleted:
- `docs/product/index.md:4` — "the cross-cutting suite strategy" → "the cross-cutting suite design"; `:9` the strategy link → `[Suite integration architecture](../proposals/suite-integration-architecture.md)` (the design that says how the products fit together; kept by step 2).
- `docs/product/reflekt/v1.md:5` — the `**Related:**` line naming the strategy document removed; `:21` "those are v2+, per the strategy doc." → "those are v2+."
- `docs/backlog.md:166` — the strategy file dropped from the done row's list; `:171` the "(e.g. product → suite-strategy)" example dropped.
- `docs/conventions/project-rules.md:33` — the deleted harness evaluation dropped from the example list.

The 22 broken references (the plan's count; my checker found 21 plus the strategy link above, the one-off difference being the plan counting the strategy link among them):
1–5. `docs/product/reflekt/v1.md:29,517,526,529,567` — `docs/architecture/v1.md` → `docs/architecture/reflekt/v1.md`
6. `docs/product/reflekt/v1.md:83` — `docs/features/CreatorAutoJoin.md` → `<reflekt-repo>/docs/features/CreatorAutoJoin.md` (the original Reflekt project; the content lives outside this repo)
7–8. `docs/product/reflekt/v1.md:195,555` — `docs/features/PhaseActionGating.md` → `<reflekt-repo>/docs/features/PhaseActionGating.md`
9. `docs/product/reflekt/v1.md:233` — `docs/features/CardGrouping.md` → `<reflekt-repo>/docs/features/CardGrouping.md`
10–11. `docs/product/reflekt/v1.md:256,359` — `docs/features/DiscussionFlowAndReporting.md` → `<reflekt-repo>/docs/features/DiscussionFlowAndReporting.md`
12–13. `docs/architecture/reflekt/v1.md:5,12` — `docs/specs/v1.md` → `docs/product/reflekt/v1.md`
14. `docs/product/reflekt/v2.md:6` — `docs/specs/v1.md` → `docs/product/reflekt/v1.md`
15. `docs/backlog.md:130` — `docs/architecture/poker-v1.md` → `docs/architecture/poker/v1.md`
16–17. `docs/backlog.md:170` — Reflekt's `docs/features/` and `docs/plans/` → `<reflekt-repo>/docs/features/`, `<reflekt-repo>/docs/plans/` (in the original Reflekt project)
18. `docs/architecture/index.md:5` — `.claude/rules/engineering-discipline.md` → "the agent plugin's `engineering-discipline` rule"
19. `docs/architecture/decisions.md:20` — the same rule file → "the agent plugin's `engineering-discipline` rule (it ships with the plugin, not in this repo)"
20. `docs/architecture/fokus/v1.md:5` — `.claude/skills/` → "the agent plugins enabled in `.claude/settings.json`"
21. `docs/conventions/project-rules.md:17` — `client/index.html`, `client/*.config.*` → `client/apps/*/index.html`, `client/apps/*/*.config.*`
22. `docs/conventions/kb-entry-schema.md:39` — example `src/Services/Fokus/Fokus.Domain/Aggregates/Sprint.cs` → `src/Services/Fokus/Fokus.Domain/Sprint/Sprint.cs`
Kept, as the plan says: the three git-ignored local files `docs/deployment-azure.md` names under `deploy/`.
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** technical: a reference to content that lives only in the original Reflekt project uses step 4's `<reflekt-repo>/…` placeholder form rather than prose, so the file stays named. technical: the product index's empty backbone section now points at the integration proposal instead of being dropped. technical: `docs/architecture/decisions.md:112` ("the suite strategy is backbone", in ADR-023) is left as written — it names no file and the plan keeps ADR meaning; listed as a carry-over.
**Deviations:** None. Acceptance: the `suite-strategy` search over live docs prints nothing (plan time 4); step 3's search 5 stays empty; the reference checker (scratchpad, links + `@` includes + backticked `docs/`/`src/`/`client/`/`deploy/`/`.claude/`/`.github/` paths) reports only the three kept deploy files.
**Test problem:** step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it

## Step 6 — Keep personal settings out of the shared settings file
**Files:** `.claude/settings.json` — kept `$schema`, `extraKnownMarketplaces` and `enabledPlugins` unchanged; removed `permissions`, `skipDangerousModePermissionPrompt` and `skipWorkflowUsageWarning` (file rewritten whole: the editor's settings validation refuses an intermediate invalid JSON state). Only the worktree's copy changed; no setting this session runs under was touched.
**Removed block, verbatim** — for step 8 to add to the main checkout's git-ignored `.claude/settings.local.json` (only the keys that file does not already hold):
```json
  "permissions": {
    "defaultMode": "bypassPermissions",
    "allow": [
      "Bash",
      "Read",
      "Grep",
      "Glob",
      "Edit",
      "Write",
      "WebFetch",
      "WebSearch",
      "Skill",
      "Workflow",
      "Task",
      "TaskCreate",
      "TaskUpdate",
      "TaskList",
      "TaskGet",
      "TaskOutput"
    ],
    "deny": [
      "Bash(rm -rf /:*)",
      "Bash(rm -rf /*)",
      "Bash(rm -rf ~:*)",
      "Bash(rm -rf ~/*)",
      "Bash(sudo:*)",
      "Bash(git push:*)",
      "Bash(git reset --hard:*)",
      "Bash(docker rm:*)",
      "Read(./**/.env)",
      "Read(./**/.env.*)",
      "Read(./**/secrets/**)",
      "Read(./.credentials/**)"
    ]
  },
  "skipDangerousModePermissionPrompt": true,
  "skipWorkflowUsageWarning": true
```
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** None. Note for the close: the removed block also held the `deny` safety list (destructive commands, `.env` and secrets reads); it leaves the shared file with the rest and goes back to the owner's local file with it. Acceptance: the plan's `node -e` check exits 0 (plan time: 1).
**Test problem:** step run — record `skipped` (nothing to run for a settings-file change) — not fixed — nothing to fix; the complete run at close covers it

## Step 7 — Write the READMEs and the licence
**Files:** `README.md` — new: what the suite is (Fokus analytics, Reflekt retrospectives, Poker planning poker in one modular monolith with one sign-in); why the repo exists (built entirely by agents, on the architecture of the author's .NET microservices course repo); the stack as it is (.NET 10, FastEndpoints, EF Core with SQLite, MassTransit, gRPC, SignalR, Vue 3 with Pinia and Tailwind); repository layout; prerequisites; running locally (clone URL with `<github-owner>`, Google and Jira settings through `dotnet user-secrets` on `src/Host` with placeholder values, the local Google redirect URI, client build into the host's `wwwroot`, `dotnet run --project src/Host`, the three apps' paths and Vite dev servers); tests and client build; deploy pointer to `docs/deployment-azure.md`; a "How it was built" section (the agent pipeline and a guide to `docs/specs/`, `docs/proposals/`, `docs/architecture/decisions.md`, `docs/conventions/`, `docs/kb/`, `docs/product/`, `docs/backlog.md`); a licence line. `LICENSE` — new: the Reflekt repo's MIT licence copied byte for byte (2026, the owner's name). `client/apps/dashboard/README.md`, `client/apps/retro/README.md` — the stock Vite template replaced by a short app README (dev server and port, build output under `src/Host/wwwroot/`, pointer to the root README).
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** product: the README's facts were checked against the tree, not written from memory — ports and outputs from each `vite.config.ts`, the settings keys from `src/Host/appsettings.json`, the callback path from the Identity auth configuration, the database migration on start from `src/Host/Program.cs`, the first-user-becomes-admin rule from `src/Services/Identity/CLAUDE.md`. product: the plugin marketplace link (already public in `.claude/settings.json`) is the one external link to the agent tooling; the course repo is named by description only, with no link. product: Xray settings are left out of the run steps (optional integration; Jira alone is enough for Fokus's sync).
**Deviations:** None. The poker app has no README and the plan names none, so none was added. Acceptance: `Recommended IDE Setup` in `client` prints nothing (plan time 1); `README.md` and `LICENSE` are non-empty; `README.md` contains `SprintRituals.slnx`, `user-secrets`, `docs/specs/` and `How it was built`; every search of steps 3 and 4 run with `--untracked` prints nothing.
**Test problem:** step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it

## Fix Round 1
**Files:** one scratchpad script, literal replacements, each asserted to match exactly once (27 in 21 files), line endings untouched:
- `docs/specs/fokus/adhoc-SuiteInfrastructure/delivery/plan.md` — row 2: the cross-repo note now says to reference source files as `<repo>/src/...` and to build with `dotnet build <repo>/SprintRituals.slnx`, `<repo>` being this suite repo's root. The command again targets the suite repo from the other working directory, and "absolute slnx path" is true again. The same file's acceptance line and verification-table row 1 were bare `dotnet build SprintRituals.slnx` under the same cwd note; both got the `<repo>/` prefix. "scaffold at this repo" → "in this repo".
- `docs/specs/adhoc-Pass1-SkillRepair/delivery/consumption-evaluation.md` — row 3: the second personal settings folder's name → "a second personal settings file".
- row 4: `docs/specs/adhoc-ReflektSprintOwnership/delivery/questions.md:41` "the un-merged the original Reflekt project repo" → "the un-merged original Reflekt project repo"; `:45` "the existing the original Reflekt project client" → "the existing client from the original Reflekt project"; `docs/specs/adhoc-ReflektSprintOwnership/delivery/lessons.md:7` as `:41`; `docs/specs/adhoc-ReflektIdentityUnification/delivery/lessons.md:22` "**lifted the original Reflekt project migrations**" → "**migrations lifted from the original Reflekt project**"; `docs/proposals/nexus-format-gap.md:108` "The hardcoded path the reference codebase" → "The hardcoded path to the reference codebase"; `docs/specs/adhoc-Pass1-SkillRepair/delivery/implementation.md:5` the doubled "(the plugin source repo)" after "**plugin source repo**" dropped.
- row 5: `docs/architecture/decisions.md:15` "**the reference codebase** is" → "**The reference codebase** is".
- siblings found by the class check: `docs/proposals/reflekt-refactor-triage.md:74`, `docs/specs/adhoc-Pass5-EventCorrectness/delivery/{lessons.md:21,plan.md:64}` "reference project (the reference codebase)" → "reference project"; `docs/specs/adhoc-Pass1-SkillRepair/delivery/lessons.md:25`, `plan.md:230` the doubled "(the plugin source repo)" dropped; `plan.md:36` "this repo (this repo)" → "this repo"; `review.md:87` "Plugin source repo at the plugin source repo" → "Plugin source repo"; `docs/plugin-feedback/nexus-1.21.0-2026-07-04.md:4`, `nexus-2.4.72-2026-10-30.md:4` "plugin source repo (the plugin source repo)" → "plugin source repo"; `docs/specs/adhoc-ReflektFrontendMerge/delivery/implementation.md:25` lower-case sentence start → "The original Reflekt project is unmodified."; `docs/specs/fokus/adhoc-SuiteScaffolding/delivery/plan.md:14` "Fresh repo at this repo" → "Fresh repo: this repo", `:53` "Fresh this repo, private GitHub" → "This repo, fresh, private GitHub"; `docs/specs/fokus/adhoc-SuiteScaffolding/delivery/summary.md:6` "suite repo at this repo" → "suite repo (this repo)"; `docs/specs/fokus/adhoc-SuiteScaffoldingV2/delivery/plan.md:16` "Continue empty this repo" → "Continue in this empty repo"; `docs/specs/adhoc-run-app/delivery/lessons.md:75` "the original Reflekt (the original Reflekt project `DomainEventPublishingInterceptor`)" → "the original Reflekt project (its `DomainEventPublishingInterceptor`)".
**Skills:** None (fix round of wording; plan: Skill none, TDD no)
**Decisions:** technical: row 2 uses step 4's `<repo>` command placeholder and names it once in the sentence, so the record keeps saying the command ran against this suite repo from another working directory. technical: a parenthetical that only repeated the name before it was dropped; one that adds something (a citation, "separate git target") was kept.
**Deviations:** None. Rows 1 and 6 are the coordinator's, not touched. Acceptance, all re-run with `--untracked`: step 2's file-name count prints 0, the strategy file is absent, its name/report search prints nothing; step 3's six searches, step 4's two searches, step 5's `suite-strategy` search and step 7's `Recommended IDE Setup` search all print nothing; `README.md` and `LICENSE` are non-empty and the README holds its four must-contain items. `git grep -n -I -i -E '\.claude-o[m]ni'` prints nothing.
**Test problem:** step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
**Class checked:** row 2: `grep -n -E 'dotnet (build|test|run|ef|sln)'` over the two cross-repo plans whose cwd note points at another checkout (adhoc-SuiteInfrastructure, adhoc-SuiteScaffoldingV2) — siblings fixed: SuiteInfrastructure's acceptance line and verification-table row 1. ScaffoldingV2's step-4 command already reads `cd <repo> && …`, and its other commands are unchanged from the baseline.
**Class checked:** row 3: `git grep -n -I -i -E '\.claude-o[m]ni'` — none found beyond the cited line
**Class checked:** row 4: scratchpad scan of every added line of `git diff pre-cleanup` in modified `.md` files (a superset of step 4's 95 files), looking for a replacement phrase (`the original … project`, `the reference codebase`, `the plugin source repo`, `this repo`) after an article or adjective, before a noun, doubled in parentheses, or after "at"; plus a by-eye read of all 113 added lines carrying a replacement phrase — siblings fixed: the sibling lines listed above (15 lines in 13 files, one of them row 5's class) plus `docs/specs/fokus/adhoc-SuiteInfrastructure/delivery/plan.md:8` "at this repo"; 6 remaining scan hits read correctly and were kept (e.g. "the original Fokus project source", a list item starting "this repo —")
**Class checked:** row 5: the same scan's lower-case-sentence-start pattern — siblings fixed: `docs/specs/adhoc-ReflektFrontendMerge/delivery/implementation.md:25`

## Fix Round Hand-up 1
The second sweep after the merge into the default branch: work committed there after the cleanup was built still held private data. One scratchpad script (literal replacements, the step 3 and 4 mapping reused value for value, each rule asserted to its exact hit count — all 14 matched; line endings unchanged), dry-run first.
**Files:** `docs/specs/F8-AzureDeployment/delivery/implementation.md` — the go-live operator log (lines 105–121, 30 replacements on 15 lines): the tenant and subscription display names → "in the work tenant, on the subscription", the subscription id → `<subscriptionId>`; the new app → `<app-name>` (8), the old Fokus app → `<old-fokus-app>` (3), the resource group → `<resource-group>`, the new plan → `<plan-name>` (3), the old plan → `<old-plan-name>` (2), the deployment identity → `<deploy-identity-name>`, its application id → `<appId>`; the temporary worktree's machine path → `<temp>/…` (step 4's temp rule); the owner's first public IP address → `<owner-ip>` (2), the address it changed to → `<owner-new-ip>` (2); the Google Cloud project's name → "the owner's Google Cloud project". `docs/skill-gaps/registry.md:116` — the machine path into another private repo → "a cross-repo skill-gaps file in a private repo", no path. `docs/specs/F7-PokerTicketContext/delivery/review-critic-codex.md` — 11 markdown link targets into this repo → root-relative `/docs/…` (line suffixes such as `:244` kept). `docs/specs/F7-PokerTicketContext/delivery/review-critic.md:267` — the reviewed file's machine path → repo-relative `docs/specs/…/spec.md`. `docs/specs/F8-AzureDeployment/delivery/lessons.md` and `docs/specs/adhoc-run-app/delivery/lessons.md` — checked, nothing private (one generic `localhost` redirect URI), not changed.
**Skills:** None (fix round of text edits; plan: Skill none, TDD no)
**Decisions:** technical: the owner's two public addresses got two placeholders, `<owner-ip>` and `<owner-new-ip>`, so one private value maps to one neutral value and the 2026-11-03 line still says the address changed. technical: the Google Cloud project's name (a cloud name the plan's list does not name, found by an extra search) became a description, as the plan's rule for cloud display names does. technical: the GitHub repository path in the F8 records (`<github-owner>`'s real login, 5 lines incl. the operator log's federated-credential subject) stays — step 3 scoped that replacement to `docs/deployment-azure.md` and the owner's name stays as author; listed below for the architect.
**Deviations:** None. Acceptance, all with `--untracked` over the whole tree: step 2's file-name count prints 0, the strategy file is absent, its name/report search prints nothing; step 3's six searches, step 4's two, step 5's `suite-strategy` search and step 7's `Recommended IDE Setup` search print nothing; `README.md` and `LICENSE` are non-empty and the README holds its four must-contain items. Extra IPv4 search over `docs`: one hit left, `docs/proposals/azure-deployment-handoff.md:52` — `0.0.0.0:8080`, the all-interfaces bind address (non-personal). Extra machine-path search: two hits left, both `src/Services/Fokus/Fokus.Domain.Tests/Analytics/BugRatioAnalyzerTests.cs:981,990` — the `p://a/x.png` inside a fake `http://` avatar URL, not a path (app test file, not touched). My own GUID search over `docs` (not in the dispatch): left only public Micro Focus documentation URLs and Claude/Codex session ids in delivery records (`F11-PublishCleanup/delivery/plan.md:8`, `F7-PokerTicketContext/delivery/review-critic-codex.md`, `F8-AzureDeployment/delivery/{communication-log,review-critic-codex,review}.md`) — tool session ids, no cloud or personal id.
**Test problem:** step run attempt 1 (run `mwb48mir-11493057`, unit `round`) — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
**Class checked:** hand-up 1: the dispatch's two searches plus three of my own over the whole tree with `--untracked` — GUID-shaped ids in `docs`, the GitHub login in a repo path, and the Google Cloud project's name — siblings fixed: the Google project name on the operator log's line 119; listed: the GitHub repository path on 5 lines in the F8 records (Decisions above)

## Key Decisions
- Step 2 — technical: the integration proposal's § 13 sentence mentioning the strategy document stayed (proposals are not live docs).
- Step 3 — technical: ticket keys numbered `ABC-101`…`ABC-105` in order of first appearance; the old Reflekt host became `<old-reflekt-app>.azurewebsites.net`; the region name stays.
- Step 4 — technical: a path below another of the owner's repos keeps its repo-relative tail behind a named placeholder (`<fokus-repo>/…`, `<reflekt-repo>/…`, `<reference-repo>/…`, `<plugin-repo>/…`, `<private-repo>/…`); a bare repo root became the project's name; paths in this repo became repo-relative (link targets root-relative); commands got placeholders, not prose. Ruled correct by the architect.
- Step 5 — technical: content that lives only in the original Reflekt project is cited with step 4's placeholder form; the product index's backbone points at the integration proposal; ADR-023's "the suite strategy is backbone" stays as written (confirmed by the coordinator).
- Step 7 — product: README facts checked against the tree; the marketplace link is the one external tooling link; Xray left out of the run steps.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan: Skill none, TDD no (repo hygiene) |
| 2 | None | plan: Skill none, TDD no (docs) |
| 3 | None | plan: Skill none, TDD no (text edits) |
| 4 | None | plan: Skill none, TDD no (text edits) |
| 5 | None | plan: Skill none, TDD no (text edits) |
| 6 | None | plan: Skill none, TDD no (config) |
| 7 | None | plan: Skill none, TDD no (docs) |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| ADR-023 still names the deleted strategy document as backbone | low | reviewer | `docs/architecture/decisions.md:112` "the suite strategy is backbone" | Left as written to keep the ADR's meaning; the coordinator confirmed. A later doc pass may reword it. |
| Backlog row "Cross-ref cleanup" still reads **Later** | low | architect | `docs/backlog.md:171` | Step 5 fixed every broken reference in the live docs; whether the row is now done is the backlog owner's call — not changed here. |
| The removed settings block held the `deny` safety list | medium | architect | step 6 block | Goes back to the owner's local settings at the close; students opening the repo get neither the bypass mode nor the deny list. |
| Feature records keep paths inside other repos behind placeholders | low | reviewer | step 4 block | Ruled correct by the architect; listed so the reviewer reads it as settled. |
| The GitHub repository path (owner's login) stays in the F8 records | low | architect | 5 lines in `docs/specs/F8-AzureDeployment/delivery/{implementation,plan,review-critic}.md` | Step 3 replaced it only in `docs/deployment-azure.md`; the public copy will carry the same login, so likely fine — the architect's call whether the records also get `<github-owner>/<repo>`. |

## Unfinished Tests
- Step 1: none — the build-only step run passed.
- Step 2: Test problem: step run — record `skipped` (nothing to run for a docs-only change) — not fixed — nothing to fix; the complete run at close covers it
- Step 3: Test problem: step run — record `skipped` (nothing to run for a docs and example-config change) — not fixed — nothing to fix; the complete run at close covers it
- Step 4: Test problem: step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
- Step 5: Test problem: step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
- Step 6: Test problem: step run — record `skipped` (nothing to run for a settings-file change) — not fixed — nothing to fix; the complete run at close covers it
- Step 7: Test problem: step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
- Fix Round 1: Test problem: step run — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it
- Fix Round Hand-up 1: Test problem: step run attempt 1 (run `mwb48mir-11493057`, unit `round`) — record `skipped` (nothing to run for a docs change) — not fixed — nothing to fix; the complete run at close covers it

## Deviations from Plan
- None. Searches started below the plan-time counts because each step ran after the earlier ones (recorded per step). Step 8 is the close's.

*Status: COMPLETE — developer, 2026-11-10*
