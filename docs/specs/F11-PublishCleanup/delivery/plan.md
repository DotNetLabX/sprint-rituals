# F11 — Publish Cleanup

**Feature Spec:** None
**Model:** claude-fable-5-2
**Size:** 7 developer-built steps · 9 files · fullChecks: none — no fullChecks list declared
**Lane:** light — owner's level
**Build lane:** fastest — lighter checks — recommended: fastest — lighter checks
**Sessions:** 4db56324-33a0-4e6c-a12d-90222c70e0b2
**Skills per step:** on
**Mirrors:** the Reflekt repo's `F33-PublishCleanup` plan — named by the owner on 2026-11-10 ("do the same thing I did in Reflekt"); the ticket wins a conflict
**Rule list:** not applicable — plan-only definition
**Base:** d74a84f — the merge-base of the tree the plan was read in with the default branch

## Context

The repo will be shared publicly — on the owner's CV and with students — as a suite built entirely by agents on the same architecture as the owner's .NET microservices course repo. Before that the current tree must be clean: no personal or employer data, no build output or tool state, live docs that point at real files, a README and a licence, and a green build with its existing tests. Backlog row: `docs/backlog.md`, F11. Baseline tag: `pre-cleanup` (commit `d74a84f`).

**Owner scope limit (binding, carried from the mirrored plan):** cleanup only — no new features, no new tests, no app-behaviour change. The feature records under `docs/specs/`, the proposals and the research notes stay as evidence of how the suite was built: they are edited only where this plan names an edit.

**Delivery artifacts never quote what this feature removes.** The employer's names, its customers, its Jira tenant, project and ticket keys, its release and feature-area names, colleagues' names, cloud resource names, private project names and machine paths are referred to only by description ("the employer", "the resource group", "the worktree") in `plan.md`, `implementation.md`, `review*.md`, `done-check.md`, `lessons.md` and `summary.md`. Patterns use character classes so they never match their own text. The developer's bulk-replacement script or token list holds real tokens, so it lives in the scratchpad, never in the tree.

**Plan-time evidence (executed 2026-11-10 on `d74a84f`, in a clean worktree):**
- **No credential found** in any tracked file or in any of the 96 commits (known key formats and generic secret assignments). The tracked `src/Services/Fokus/Fokus.API/deploy.zip` (21 MB) was opened: its settings files hold placeholders only. No secret needs rotating.
- **Baseline green:** `verify-run.js --profile complete` with the Google and Jira settings blanked — pass, 339 tests in 6 projects, 26 s. `npm ci` and `npm run build` in `client/` — exit 0 (6 high-severity package advisories reported by `npm ci`).
- **Tracked build output and tool state (50 files):** `src/Services/Fokus/Fokus.API/deploy.zip`; `src/Services/Fokus/Fokus.API/wwwroot/` (old build output; no project or source file references it — the Host serves `src/Host/wwwroot/`, already git-ignored); `client/apps/retro/.omc/` and `docs/specs/fokus/adhoc-SuiteScaffoldingV2/delivery/.omc/` (state of an older agent tool, with a machine path).
- **Private references:** counted by the acceptance searches of steps 3 and 4 (employer 71 lines, Jira and product names 18, colleagues 13, cloud names 85, private projects 109, machine paths 599). Outside `docs/` the only hits are `CLAUDE.md`, `src/Services/Identity/CLAUDE.md`, `deploy/deploy-config.example.ps1` and the tool-state folder. No app source, test, seed, settings or workflow file holds one (confirmed by both plan reviewers).
- `docs/proposals/suite-integration-architecture.md` ends with a pasted retro report (from its `# Sprint 28` heading) naming the employer's team and nine people.
- `deploy/deploy.ps1` and `deploy/provision.ps1` already throw when no target is given, and `.github/workflows/deploy.yml` reads every name from repository variables: only the example config file carries real names.
- `.claude/settings.json` carries the owner's personal permission mode (no permission prompts), which anyone opening the repo in Claude Code would inherit.
- No root `README.md` and no `LICENSE`; `client/apps/dashboard/README.md` and `client/apps/retro/README.md` are the stock Vite template.
- **Live docs hold 22 references that name no file** (old Reflekt paths, the removed in-repo agent rules, moved architecture pages) — listed in step 5.
- The default branch's checkout holds uncommitted edits to `.claude/settings.json` (one plugin switch) and `docs/backlog.md`, both files this plan changes.

**Owner actions outside F11 (not developer work; listed in `summary.md`):**
1. Publish only a history-scrubbed fresh copy — old commits keep everything this feature removes. The scrub runs on the owner's word, on a fresh clone; this private repo is never rewritten.
2. Never push the `pre-cleanup` tag to a public remote.
3. Check the GitHub side before publishing: repository variables, the extra remote branch, and a secret-scanner run over the fresh copy (none is installed on this machine).

## Scope

**In scope:** remove build output and tool state; `.gitignore` fixes; remove the private documents named in step 2; replace employer, people, cloud and private-project references and machine paths; fix references in live docs; move the personal permission settings out of the shared settings file; write the READMEs and the licence.

**Out of scope:** the git-history scrub and the publish itself (one-way, owner's word); new tests; app-behaviour changes; package upgrades for the six advisories; editing old feature records beyond the named replacements; the unfinished poker work on the default branch; the untracked `.claude/verify.json` and `.claude/nexus-agents.json` (another session's pending files — never committed by this feature).

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | files to delete, ignore rules | — (repo hygiene, expected) |
| 2 | (none) | — | no | documents to delete or trim | — (docs, expected) |
| 3 | (none) | — | no | replacement rules per class | — (text edits, expected) |
| 4 | (none) | — | no | machine-path replacement rules | — (text edits, expected) |
| 5 | (none) | — | no | live-doc reference fixes | — (text edits, expected) |
| 6 | (none) | — | no | shared settings file | — (config, expected) |
| 7 | (none) | — | no | READMEs, licence | — (docs, expected) |
| 8 | (none) | — | no | final sweep, complete run | — (close-owned) |

TDD is `no` throughout: no step adds behaviour.

## Domain Model Changes

None.

## Data Model Changes

None. No migration.

## Implementation Steps

Deletions are plain file deletes — never `git rm` (staging is a git write). Searches below run on the working tree, so a deleted file no longer counts.

### Step 1 — Remove build output and tool state; fix `.gitignore`
- Delete: `src/Services/Fokus/Fokus.API/deploy.zip`, `src/Services/Fokus/Fokus.API/wwwroot/`, `client/apps/retro/.omc/`, `docs/specs/fokus/adhoc-SuiteScaffoldingV2/delivery/.omc/`.
- Files: `.gitignore` — add unanchored rules `.omc/`, `deploy.zip`, `src/Services/*/*.API/wwwroot/`, `.claude/.worktree-target`, `.claude/approval-scope/`, `.claude/pipeline-state/`. Existing rules stay.
- **Effort points:** 2
- **Accept:** `find src/Services/Fokus/Fokus.API/wwwroot client/apps/retro/.omc docs/specs/fokus/adhoc-SuiteScaffoldingV2/delivery/.omc src/Services/Fokus/Fokus.API/deploy.zip -type f 2>/dev/null | wc -l` prints 0 (plan time: 50). Ignore proof, expected no output — `--no-index` because the deleted files stay in the index until the close commits: `for p in src/Services/Fokus/Fokus.API/deploy.zip src/Services/Fokus/Fokus.API/wwwroot/x client/apps/retro/.omc/x .claude/approval-scope/x .claude/.worktree-target; do git check-ignore -q --no-index "$p" || echo "NOT IGNORED: $p"; done`; each of the six rules is a line of the committed `.gitignore`.

### Step 2 — Remove the private documents
- Delete the evaluation of an employer-internal harness — the three items under `docs/` whose names carry its abbreviation (file-name pattern `*v[w]h*`): one proposal, one research note, one feature-record folder.
- Delete `docs/product/suite-strategy.md` (business strategy, not product documentation).
- Files: `docs/proposals/suite-integration-architecture.md` — delete everything from the `# Sprint 28` heading to the end of the file; the design above it stays.
- **Effort points:** 1
- **Accept:** `find docs -iname '*v[w]h*' | wc -l` prints 0 (plan time: 3); `test ! -e docs/product/suite-strategy.md`; `git grep -n -I -i -E 'ciob[a]nu|Sprint 28 [^.] Sprint Report'` (plan time: 4) prints nothing.

### Step 3 — Replace employer, people, cloud and private-project references
The site list **is** the output of the five searches below, run over every tracked file after step 2 — do not hand-curate it. Rules, same shape, no real data:
- Employer names → "the employer" in prose, `acme` inside identifiers; its Jira tenant host → `acme.atlassian.net`; its project key → `ABC`; each ticket key → one fixed neutral key (`ABC-101`, `ABC-102`, …), the same key everywhere; its release and feature-area names → neutral ones ("Release A", "feature area B"), the same name everywhere.
- Colleagues → role labels ("QA engineer A", "Developer B", …), one label per person everywhere. The owner's own name stays (author).
- Cloud names → placeholders: the app `<app-name>`, the old apps `<old-fokus-app>` and `<old-reflekt-app>`, the resource groups `<resource-group>` and `<old-resource-group>`, the plans `<plan-name>` and `<old-plan-name>`, the deployment identity `<deploy-identity-name>`, the tenant and subscription display names → "the work tenant", "the subscription". Commands in `docs/deployment-azure.md` must still read as runnable with the placeholders filled in; its two lines naming the GitHub repository get `<github-owner>/<repo>`.
- Private project names → "an employer-internal harness", "a private repo".
- Files: `deploy/deploy-config.example.ps1` — both values become placeholders (`<app-name>`, `<resource-group>`); every other site is under `docs/`.
- **Effort points:** 8
- **Accept** (each separately; expected no output):
  1. `git grep -n -I -i -E 'o[m]nishelf|o[m]niaz|a[l]famart'` (plan time: 71)
  2. `git grep -n -I -E '\bP[D]-[0-9]{3,}\b|(^|[^A-Za-z0-9-])P[D]($|[^A-Za-z0-9-])|Bl[u]e Ocean|Gr[e]en Bean|on-device P[O]G'` (plan time: 18)
  3. `git grep -n -I -w -E 'T[a]ylor|M[a]teusz|P[a]we.|O[v]idiu|N[o]rbert|C[^.]lin|M[a]ciej|A[d]am|Ciob[a]nu|O[l]eksandr|A[n]drzej|A[l]icja|G[r]zegorz'` (plan time: 13)
  4. `git grep -n -I -E 'internal-apps-r[g]|sprint-rituals-pla[n]|-o[m]niaz|reflekt\.azurewebsite[s]|Azure Sponsorshi[p]|fokus-pla[n]|reflekt-r[g]|github-deplo[y]'` (plan time: 85)
  5. `git grep -n -I -i -E 'c[o]de-forge|virtual[- ]worker|\bv[w]h\b|o[m]nishelf-doc[s]'` (plan time: 109)
  6. `git grep -n -I 'ldumi[t]/' -- docs/deployment-azure.md` (plan time: 2)

### Step 4 — Replace machine paths
- A path inside this repo → repo-relative. A path into another of the owner's repos → that project's name with no path ("the original Fokus project", "the reference codebase", "a private repo"). A home, temp or session-transcript path → `<home>`, `<temp>`, "the session transcript". Generic system paths that name no person or project (a standard program folder, an elided example) stay.
- Files: `CLAUDE.md` (the Reference Projects table keeps its three rows, the paths replaced by names and the note that those repos are not part of this one), `src/Services/Identity/CLAUDE.md`; every other site is under `docs/`.
- Write any script that handles backslashes with the Write tool, not a shell heredoc.
- **Effort points:** 8
- **Accept** (each separately; expected no output): `git grep -n -I -i -E '[A-Za-z]:[\\/]+(src|temp|tmp|o[m]nishelf|tools|caches)\b|/[a-z]/(src|temp|tmp)/|[A-Za-z]:[\\/]+Users[\\/]|[A-Za-z]--src-'` (plan time: 599); `git grep -n -I -i -E 'd[u]mitrescu\.|@g[m]ail\.com'` (plan time: none).

### Step 5 — Fix references in live docs
Live docs = `CLAUDE.md`, `docs/conventions/**`, `docs/architecture/**`, `docs/product/**`, `docs/kb/**`, `docs/backlog.md`, `docs/deployment-azure.md`. Feature records and proposals are not touched here. A broken reference is a relative markdown link, an `@` include, or a backticked path starting `docs/`, `src/`, `client/`, `deploy/`, `.claude/` or `.github/` that names no file in the tree.
- Remove or reword every mention of a document step 2 deleted (plan-time sites: `docs/product/index.md`, `docs/product/reflekt/v1.md`, `docs/backlog.md` — 2 lines).
- Fix the 22 references broken at plan time, pointing each at the file that holds the content today or rewording it where the content lives outside this repo: `docs/product/reflekt/v1.md` (10: the old architecture page and four old feature pages), `docs/architecture/reflekt/v1.md` (2) and `docs/product/reflekt/v2.md` (1: the old product-spec path), `docs/backlog.md` (3), `docs/architecture/index.md` and `docs/architecture/decisions.md` (1 each: a rule file that now ships with the agent plugin), `docs/architecture/fokus/v1.md` (1), `docs/conventions/project-rules.md` (1), `docs/conventions/kb-entry-schema.md` (1). The three git-ignored local files `docs/deployment-azure.md` names under `deploy/` are correct and stay.
- **Effort points:** 5
- **Accept:** `git grep -n -I 'suite-strategy' -- CLAUDE.md docs/conventions docs/architecture docs/product docs/backlog.md docs/kb` (plan time: 4) prints nothing; step 3's search 5 stays empty; `implementation.md` lists each of the 22 references with its new target or wording; the close re-runs the reference list (step 8).

### Step 6 — Keep personal settings out of the shared settings file
- Files: `.claude/settings.json` — keep `$schema`, `extraKnownMarketplaces` and `enabledPlugins`; remove `permissions`, `skipDangerousModePermissionPrompt` and `skipWorkflowUsageWarning`. Report the removed block verbatim in `implementation.md` so the close can restore it locally (step 8).
- **Effort points:** 1
- **Accept:** `node -e "const s=JSON.parse(require('fs').readFileSync('.claude/settings.json','utf8'));const bad=['permissions','skipDangerousModePermissionPrompt','skipWorkflowUsageWarning'].filter(k=>k in s);const miss=['\$schema','extraKnownMarketplaces','enabledPlugins'].filter(k=>!(k in s));if(bad.length||miss.length){console.log('bad',bad,'missing',miss);process.exit(1)}"` exits 0 (plan time: exits 1, three keys present).

### Step 7 — Write the READMEs and the licence
- Files: `README.md` (new) — must contain: what the suite is (sprint analytics, retrospectives and planning poker in one modular monolith); why the repo exists (built entirely by agents, on the architecture of the owner's .NET microservices course repo); the stack as it is (.NET 10, FastEndpoints, EF Core with SQLite, MassTransit, gRPC, SignalR, Vue 3 with Pinia and Tailwind); prerequisites; how to run locally — Host, the three client apps, Google sign-in and Jira settings through user-secrets with placeholder values; how to run the tests and the client build; how to deploy (pointer to `docs/deployment-azure.md`); a **"How it was built"** section — the agent pipeline, with a guide to `docs/specs/` (one folder per feature: definition and delivery records), `docs/proposals/`, `docs/architecture/decisions.md`, `docs/conventions/`, `docs/kb/`; a clone URL with a placeholder owner; a licence line.
- Files: `LICENSE` (new) — MIT, 2026, the owner's name (same text as the Reflekt repo's licence).
- Files: `client/apps/dashboard/README.md`, `client/apps/retro/README.md` — replace the stock template with a short app README (dev server, build output into the Host's `wwwroot`, pointer to the root README).
- **Effort points:** 5
- **Accept:** `git grep -n -I 'Recommended IDE Setup' -- client` (plan time: 1) prints nothing; `test -s README.md && test -s LICENSE`; `README.md` contains `SprintRituals.slnx`, `user-secrets`, `docs/specs/` and `How it was built`; the searches of steps 3 and 4, run with `--untracked`, stay empty. The code reviewer checks each must-contain item by reading.

### Step 8 — Final sweep, complete run, local settings (Owner: close)
Done by the closing main session, as the last acts before the commit:
- re-run every search of steps 2–5 with `--untracked`, the delivery folder included, and the live-doc reference list of step 5;
- run the complete suite and the client build with the Google and Jira settings blanked;
- add step 6's removed keys to the git-ignored `.claude/settings.local.json` of the main checkout — only keys that file does not already hold; every key it holds stays as it is — so the owner's own sessions behave as before;
- stage only this feature's files: `.claude/verify.json` and `.claude/nexus-agents.json` stay out of the commit;
- the merge into the default branch happens only on the owner's word; where it meets the owner's uncommitted edits to `.claude/settings.json` and `docs/backlog.md`, both sides are kept.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| No app source or test changes | `git diff --name-only pre-cleanup -- src client ':(exclude)src/Services/Fokus/Fokus.API/deploy.zip' ':(exclude)src/Services/Fokus/Fokus.API/wwwroot' ':(exclude)client/apps/retro/.omc'` lists exactly `src/Services/Identity/CLAUDE.md` and the two client READMEs | pinned |
| App behaviour | complete suite (339 tests) and client build green at close, settings blanked | pinned |
| Deploy script and workflows | `git diff --name-only pre-cleanup -- deploy .github` lists exactly `deploy/deploy-config.example.ps1` | pinned |
| Feature records keep their content apart from named replacements | `git diff --name-only pre-cleanup -- docs/specs ':(exclude)docs/specs/F11-PublishCleanup'` lists only files hit by the searches of steps 3–4, plus step 2's deletions and step 1's tool-state folder | pinned |
| Architecture decisions keep their meaning | the `docs/architecture/` diff touches only lines hit by the searches of steps 3–4 or listed among step 5's references — read by the code reviewer | pinned |

## Testing Strategy

No new tests. Proof is the existing suite and the client build, green at the close with credentials blanked, plus the searches above.

## KB Impact

None — `docs/kb/` entries change only where a search of steps 3–5 hits them; no rule changes.

## Decisions

| Decision | Kind | Why | Rejected alternative | Confidence | Status |
|---|---|---|---|---|---|
| Departs from the Reflekt cleanup plan: delete the evaluation of the employer-internal harness (a proposal, a research note, a feature-record folder) | business | It describes an employer's private code and names its people; redaction would leave little worth reading. The Fokus cleanup removed the same class of document | Redact and keep | high — same ruling as in the Fokus cleanup | decided |
| Departs from the Reflekt cleanup plan: delete the product strategy document | business | Business strategy, not how the suite was built; the Fokus cleanup removed its copy | Keep it as part of the story | medium — the owner may want the strategy public | decided |
| Adds: personal permission settings leave the shared settings file | technical | A student opening the repo would inherit a no-prompt permission mode; the close restores it locally for the owner | Leave the file as it is | high — no change for the owner, safer for students | decided |
| Departs from the Reflekt cleanup plan, required by the plan-time evidence: three of its steps have nothing to do here — no test or build repair (the baseline is green), no deploy-script change (both scripts already refuse a missing target), no test-declaration commit (that file is another session's pending work) | technical | Each was measured at plan time; see Context | Repeat the steps anyway | high — measured, and confirmed by both plan reviewers | decided |
| Plugin feedback files stay, redacted | product | They show the pipeline being used and improved; after steps 3–4 they hold no employer data | Delete the folder | medium — they concern the plugin, not the app | decided |
| The pasted retro report is cut from the integration proposal; the proposal stays | technical | The design is a live reference of the architecture docs; the report is personal data pasted below it | Delete the whole file, as the Fokus cleanup did | high — the live docs link to it | decided |
| Package advisories are not fixed here | technical | Upgrades change runtime; this feature is cleanup | Upgrade now | medium — six high advisories stay visible | deferred |
| The owner's name and GitHub handle stay in records; the README's clone URL and the runbook's repository lines get a placeholder owner | product | Author identity is intended; the public repo's home is decided at publish | Name the current private repo | high — same as the mirrored plan | decided |
| No `Limit` header line | technical | The plan builds no check and no script that writes shared state; step 8's one hand edit of a git-ignored local file adds missing keys only | Add the line | medium — a plan reviewer asked for it | decided |

## Decisions and why

**P1 — Mirror the earlier cleanup.** By: owner, 2026-11-10. "What you need to do, is to do the same thing I did in Reflect or Focus repo. It was in Reflect. I did the exact same thing before making it public." Architect's reading: clean the current tree in one feature; the history scrub and the publish stay outside it, on a fresh copy, on the owner's word; the commit author identity stays as it is (ruled for all repos in the Fokus cleanup, 2026-10-13).

**P2 — Name, place, review and lane.** By: owner, 2026-11-10: the feature is F11-PublishCleanup; it runs in its own worktree and branch because the default branch holds unfinished poker work; the plan gets two reviewers, one round; the build runs in this session with lighter checks.

**P3 — Departures and fixes.** By: owner, 2026-11-10 (question box at approval): the employer-harness evaluation and the product strategy document are deleted; the personal permission settings leave the shared settings file; the three mirrored steps with nothing to do are skipped; the review fixes get a second round beside the build.

## Open Questions

None.

## Plan Review

Round 1 (light lane): Codex primary NO-GO + Sonnet second reader ACCEPT; merged 16 findings (3 HIGH, 9 MEDIUM, 4 LOW), 14 fixed in this revision, 2 waived with their reasons. Round 2 on the fixes (owner-picked, beside the build): Codex GO, all 16 closed, no new finding. Record: `review-critic.md` (merge and fold), `review-critic-codex.md`.

## Plan check

After the round-1 fold (2026-11-10):
plan-check: 0 findings, 3 searches run, 0 not run — searches compare with plan-time results; run before the build
