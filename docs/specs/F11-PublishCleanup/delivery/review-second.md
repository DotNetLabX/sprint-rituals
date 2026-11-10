# F11 — Publish Cleanup — Second reader

## Verdict: COMMENT

Scope (light lane): steps 3, 4, 5, 6 and 7 and what they touch; the skills check over every step. Not touched: `review.md` (primary's file).

## Reviewed By
reviewer, second reader.
**Model:** claude-sonnet-5-5

## Pre-commitment Predictions
- A search-clean tree still holds a sibling form of an employer or machine identifier no plan search spells: found one (Open Question 1).
- A bulk replacement leaves one private value with two neutral values, or two values merged: not found — the five ticket keys pair one-to-one in a before/after check of every changed line, and the cloud placeholders are used the same way in the runbook, the ADR and the deploy example.
- A runbook command stops reading as runnable after replacement: not found.
- The README states a port, key or path the tree does not hold: not found.
- A delivery artifact re-quotes what the feature removes: found, in the coordinator's risky-steps line (Finding 1).

## Findings

### [MEDIUM] The risky-steps line re-introduces a removed private token into the tree
**File:** `docs/specs/F11-PublishCleanup/delivery/review.md:5`
**Origin:** implementation (tool output pasted by the coordinator; the plan's binding rule says delivery artifacts never quote what the feature removes)
**Issue:** The `Unplanned:` list prints the paths of the files step 2 and step 1 deleted, and those paths carry the employer-internal harness's abbreviation (a research note, a proposal, a feature-record folder's files). Step 3's search 5, run with `--untracked` as the plan's acceptance and step 8's sweep require, returns exactly this one line (1 hit); every other search of steps 2 to 5 and 7 returns 0. It also prints the deleted `wwwroot` and `.omc` file lists, which are harmless but make the line about 6 KB long.
**Fix:** Coordinator rewrites line 5 without the deleted paths (for example the counts per folder: "95 docs files, 15 tool-state files, 31 old build-output files"), then re-runs search 5 with `--untracked`. The architect's done check already notes the same cause.
**Confidence:** 95/100

## Positive Observations
- All acceptance searches of steps 2 to 5 and 7 run with `--untracked`: 0 hits each, except the one above. `Recommended IDE Setup` in `client`: 0. File-name search for the harness abbreviation under `docs`: 0. Strategy-file mentions in live docs: 0 (the remaining hits are in a proposal, a plugin-feedback record and the delivery folder, which are not live docs).
- Beyond the plan's searches: no drive-letter path of any kind other than a standard program folder in a review record; no `/Users/`, `/home/<name>`, `%USERPROFILE%`, session-transcript file names; no UUID except vendor doc links; no personal e-mail; no credential formats; no tracked `.db`, `.pem`, `.pfx`, `.env`, `.log`, zip or `node_modules`; no company-suffix names; no hostname other than `acme`, `<app-name>` and the course's example domains.
- Mapping consistency (mechanical): every changed line pairing old and new ticket keys was checked by script; the 5 old keys map to `ABC-101` to `ABC-105`, one new key per old key and one old per new, in all files. Colleague labels: only "QA engineer A" and "Developer B", used 3 times each, no clash. Cloud placeholders are consistent across `docs/deployment-azure.md`, ADR-024, the F8 records and the deploy example (old Fokus app, old plan and old resource group keep distinct placeholders from the new ones).
- `docs/deployment-azure.md`: all `az` commands, the `Set-Content` credential JSON, the role scope and the `provision.ps1` references still parse as runnable with the placeholders filled in; the subscription line now takes `"<subscription>"`.
- Must-NOT-Change diffs (run against the `pre-cleanup` tag): app source and tests — exactly `src/Services/Identity/CLAUDE.md` and the two client READMEs; `deploy` and `.github` — exactly `deploy/deploy-config.example.ps1`; `docs/architecture/` diff is 8 lines in 4 files, all in search-hit lines or step 5's list, and the ADR meaning is intact (ADR-002 now says "derive the pattern from it", ADR-024 target and carry-over lines only swap names for placeholders).
- Step 6: the plan's `node -e` check exits 0 (no removed key left, three kept keys present); the file is valid JSON, and the kept marketplace URL is the public plugin repository. Step 3's example config holds only `<app-name>` and `<resource-group>`; `deploy.ps1` and `provision.ps1` read it only as plain strings, and both still throw on a missing target.
- Step 5 reference check, own script over 39 live markdown files (relative links, root-relative links, backticked `docs/ src/ client/ deploy/ .claude/ .github/` paths, line suffixes stripped): 3 hits, exactly the three git-ignored local deploy files the plan keeps. The 22 listed references are present in `implementation.md` with targets that exist.
- README, each claim checked against the tree: `SprintRituals.slnx` and `dotnet test SprintRituals.slnx`; host URL `http://localhost:5100` (appsettings and launch settings); callback `/auth/callback` (`AuthConfiguration.cs:45`); the four database files under `data/` and `Migrate<>` at start (`Program.cs:47-50`); Scalar only in Development (`Program.cs:94-97`); the user-secrets keys `Google:ClientId/ClientSecret` and `Jira:InstanceUrl/Email/ApiToken/ProjectKey` match `appsettings.json` and `JiraOptions`; the `user-secrets` project is `src/Host` (it has a user-secrets id); dev ports 5171/5172/5173 and output folders `src/Host/wwwroot/{dashboard,retro,poker}` match each `vite.config.ts`; `dev:*` and `build` scripts exist in `client/package.json`; Node range matches Vite 8's engines; the layout block matches `ls`; first sign-in becomes administrator (Identity CLAUDE.md, bootstrap rule); `docs/specs/fokus` and `docs/specs/reflekt` exist.
- README must-contain items of step 7: what the suite is, why it exists, stack, prerequisites, local run (Host, three apps, Google and Jira user-secrets with placeholders), tests and client build, deploy pointer, "How it was built" with the specs/proposals/decisions/conventions/kb guide, clone URL with `<github-owner>`, licence line, `SprintRituals.slnx`, `user-secrets`, `docs/specs/`: all present. The README names no employer, colleague or machine path. `LICENSE` is a 1,098-byte MIT text, 2026, the owner's name. The two app READMEs replace the stock template and their ports and output folders are true.
- Skills check: every step maps `Skill: None`, each warranted (repo hygiene, text edits, config, docs); no installed skill covers a publish cleanup (the developer filed that as a skill gap). Step 6 edits a settings file, which the built-in settings skill covers, but the step only deletes three keys, so no pattern was departed from.
- Comment-noise check: no code comments added (docs only); the `.gitignore` heading comment is one plain line.

## Gaps
- The owner's handle stays in the F8 records (federated-credential subject, remote URL) by the plan's recorded decision; only the runbook and README use `<github-owner>`. Noted, not a finding.
- `docs/product/reflekt/v1.md` now points at `<reflekt-repo>/docs/features/...` in 8 places. A public reader cannot open these; the plan allows rewording to content outside the repo, and the text says "the original Reflekt project" in the first mention only. Acceptable; a one-line note at the top of that file would help. Not a finding.

## Carry-Over Findings (from `implementation.md`)
1. ADR-023 `docs/architecture/decisions.md:112` ("the suite strategy is backbone") — **confirmed, LOW, settled**: it names no file, so it is not a broken reference; the same phrase sits once in a plugin-feedback record. A later doc pass may reword it.
2. Backlog "Cross-ref cleanup" row still **Later** — **confirmed, LOW**, the architect's call. My own reference check over the live docs finds no broken reference apart from the three kept local files, so the row is now stale.
3. Removed `deny` safety list — **confirmed**: the shared file no longer holds `permissions` or the two skip flags (check exits 0); students inherit default prompts, which is the safer side. The block is reported verbatim in step 6 for the close to restore into the git-ignored local file. Nothing else in this tree depended on those keys.
4. Records keep repo-relative tails behind named placeholders — **confirmed as settled** (architect ruling). Sampled about 30 changed lines in feature records: each still reads correctly, no doubled wording remains except one line that reads fine ("original Fokus project", no repeat).

## Open Questions
1. [LOW, confidence 60] `docs/specs/adhoc-Pass1-SkillRepair/delivery/consumption-evaluation.md:85` names a second personal Claude config folder (`~/.claude-…` with a short alias that is the employer's brand abbreviation) and two sibling references to a "twin" of the plugin built for that account appear in the same feature's records. No plan search spells this form. It identifies an account alias, not a person or a secret; the architect decides whether to neutralise the alias (suggest `~/.claude-<profile>/settings.json`) or accept it.
2. [LOW, confidence 90] `docs/architecture/decisions.md:15` now starts a sentence with a bold lower-case "the reference codebase". Suggest "The reference codebase" or "**The reference codebase**". Style only.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass (handover, tied to changed files; no tied test, `unit` role ran) | `.claude/audit/verify-verdict.json`, last line for slug F11-PublishCleanup, tree = this worktree | verdict recorded 2026-11-10T19:07Z; cited, suite not re-run |
| Acceptance searches 3.1 to 3.5, 3.6, 4 (both), 2, 5, 7 | pass; 3.5 one hit in `review.md:5` (Finding 1) | `git grep --untracked -n -I` with the plan's patterns | 0 hits each, 3.5 = 1 |
| Must-NOT-Change diffs | pass | `git diff --name-only pre-cleanup -- …` for src/client, deploy/.github | as the plan lists |
| Step 6 plan check | pass | `node -e` from the plan | no output, exit 0 |
| Ticket-key mapping | pass | scratch script over `git diff pre-cleanup -U0` | 5 old keys to 5 new keys, one-to-one |
| Live-doc references | pass | scratch script, 39 files | 3 broken = the 3 kept local files |
| README facts | pass | read against `vite.config.ts`, `appsettings.json`, `Program.cs`, `AuthConfiguration.cs`, `client/package.json` | all true |

*Status: COMPLETE — reviewer, 2026-11-10*
