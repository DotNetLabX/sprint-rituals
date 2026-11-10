# F11 — Publish Cleanup — Summary
Mode: architect-led fast lane (light, plan only)
Gear: none

## Status: COMPLETE

## What Was Built
- The current tree is cleaned for public release: build output and old tool state removed and git-ignored; the private documents deleted; employer, colleague, Jira, cloud and private-project names replaced by neutral placeholders; machine paths replaced; references in live docs repaired; the owner's personal permission settings moved out of the shared settings file; a root README, a licence and two app READMEs written. No app source or test changed.

## Key Outcomes
- 117 files modified (docs, records, `CLAUDE.md`, `.gitignore`, `.claude/settings.json`, the example deploy config, two client READMEs), 61 deleted (50 build-output and tool-state files, the private documents), 2 new (`README.md`, `LICENSE`) plus this feature's records.
- Build: the solution builds; `npm run build` in `client/` exits 0.
- **Full suite:** pass — 16 s (recorded 2026-11-10T19:23:02Z; complete red on a fast-green tree: no; filtered: no). 339 tests in 6 projects, run with the Google and Jira settings blanked, so a clean clone does not depend on this machine's stored credentials.
- **Tests and builds:** not measured — the timing program was not run at this close.
- **Map delta:** none
- **Risky steps:** Step 3 (recheck: shared with Step 4), Step 4 (recheck: security), Step 5 (recheck: shared with Step 4), Step 6 (recheck: shared with Step 5), Step 7 (recheck: shared with Step 4), item src/Services/Fokus/Fokus.API/wwwroot/assets/LoginView-HBk0iTf4.js (security), item src/Services/Fokus/Fokus.API/wwwroot/assets/auth-DEoVD9np.js (security), item src/Services/Fokus/Fokus.API/wwwroot/assets/authStore-CueR_-tb.js (security) · Unplanned: 144 deleted files — the tool-state, old build-output and private-document files that plan steps 1 and 2 remove by folder (given as a count: some of their names carry a name this feature removes)
- Review: plan review round 1 (Codex NO-GO + Sonnet ACCEPT → 16 findings, 14 fixed, 2 waived with reasons) and an owner-picked round 2 on the fixes (Codex GO, all 16 closed); done check PASS (check 1); code review Codex NO-GO (1 HIGH, 1 MEDIUM, 2 LOW) + second reader COMMENT (1 MEDIUM, 2 LOW); one fix round (cycle 1/1) carrying all six rows; light lane — no follow-up review.
- Final sweep, run by the close over the whole tree with untracked files included: every privacy search of the plan prints nothing; the live-doc reference check finds only the three git-ignored local deploy files the plan keeps.

## Deviations from Plan
- None in the build. The plan's own step 2 search was corrected at the code review (it matched its own text).

## Notes
- **Fix round items:** row 1 (HIGH, the coordinator's review record repeated a removed name in a generated file list) and row 6 (LOW, the plan's self-matching search) were fixed by the coordinator and re-verified by the final sweep. Rows 2–5 (a replaced build command, a personal settings folder name, ungrammatical sentences, a lower-case sentence start) are reported fixed by the fix-round developer — fixed, not re-verified by a reader (light lane); the developer's own scan fixed 16 more lines of the same kind.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
- **Left out:** none
- **Slow tests kept:** none
- **Timed out, sent to solo:** none
- **Flaky — passed when re-run alone:** none — **Backlog row:** none
- **Known open, not part of this feature:** six high-severity package advisories reported by `npm ci`; the decisions register still mentions the suite strategy by name (no file reference); the backlog's cross-reference cleanup row is still marked Later.
- **For the owner to decide:** the removed settings block also held the safety list (it blocks destructive commands and reading secret files); students do not inherit it. Keeping only that list in the shared file is an option.
- **Owner actions outside F11:** (1) publish only a history-scrubbed fresh copy — old commits keep everything this feature removed, including a 21 MB build archive; this private repo is never rewritten; (2) never push the `pre-cleanup` tag to a public remote; (3) check the GitHub side before publishing — repository variables, the extra remote branch — and run a secret scanner over the fresh copy (none is installed on this machine).
- **Local settings:** the close writes the removed permission keys into the main checkout's git-ignored local settings file, so the owner's own sessions behave as before after the merge.
- **Usage record:** complete — `usage-record.json`.
- **Merge:** asked at the close; the work is committed on branch `F11-PublishCleanup` in the worktree. The default branch's checkout holds uncommitted poker work and uncommitted edits to the shared settings file and the backlog — both sides are kept at the merge.
