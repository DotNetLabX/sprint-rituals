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
- **Safety list (owner, 2026-11-10, at the close):** the shared settings file keeps the list that blocks destructive commands and reading secret files, without the block on pushes; the permission mode, the allow list and the two prompt switches stay out of it. This narrows plan step 6, whose check for the permissions key no longer applies as written.
- **Owner actions outside F11:** (1) rewrite the history before publishing — old commits keep everything this feature removed, including a 21 MB build archive. Corrected 2026-11-10 on the owner's word and the Reflekt repo's own state: there the whole history was rewritten in place on 2026-11-04, with dates, authors and messages kept and new commit ids, after a full backup (a bare copy and a bundle with its checksum). The plan's reading "a fresh copy, this repo never rewritten" came from the Fokus record and is withdrawn. The rewrite follows the merge and the second sweep, as its own piece of work; (2) never push the `pre-cleanup` tag to a public remote; (3) check the GitHub side before publishing — repository variables, the extra remote branch — and run a secret scanner over the fresh copy (none is installed on this machine).
- **Local settings:** the close writes the removed permission keys into the main checkout's git-ignored local settings file, so the owner's own sessions behave as before after the merge.
- **Usage record:** complete — `usage-record.json`.
- **Merge:** not done — owner, 2026-11-10: the branch `F11-PublishCleanup` merges after the unfinished poker work on the default branch is committed. Five files overlap that uncommitted work (the shared settings file, the backlog, three feature records): both sides are kept at the merge. The uncommitted and untracked work on the default branch still holds private names (11 added lines, 3 untracked files), so the privacy searches of plan steps 3–4 are re-run after the merge, before any publish.

## After the Close (2026-11-10)
- **Merge and second sweep:** the unfinished poker work on the default branch was committed after a green build and test run, the branch was merged, and the privacy searches were re-run over the merged tree. The second sweep removed what they still found: cloud resource names and ids, two address literals and machine paths.
- **History rewrite — done, on the owner's word, the way the Reflekt repo was done:** the whole history was rewritten in place with the single-file `git filter-repo`, limited to the commits after the first one so the first commit keeps its id. All 104 commits are kept, with the same author, the same author date and commit date to the second, and the same merge shape; only the commit ids changed. Removed from every commit: the build archive, a tool's state folder, the three evaluation documents of an employer-internal tool and the product strategy document. Replaced in every file version and commit message: the private names, ticket keys, cloud ids, addresses and machine paths this feature removed from the tree.
- **Owner-accepted exception:** three commit titles changed, because they held a removed name, and five commit bodies — three for the same reason, two only because a commit id they cite was renumbered. The owner ruled that these stay as rewritten.
- **Checks before the push:** every commit compared old against new (author, both dates, message, parent count); the final tree identical to the cleaned tree; every file version, the contents of archives, every commit message and every author address scanned for the removed values and for credential formats — nothing found.
- **Backups:** a bare copy of the repository and a bundle of the original history with its checksum, made before the rewrite and kept outside the repository. They hold everything that was removed and are never pushed.
- **Push:** made by the owner, to the same GitHub repository, with a lease on the old tip, so the repository's creation date and the commit dates stay as proof of when the work was done. The one extra remote branch was deleted. A fresh download was scanned again and is clean (about 5.6 MB).
- **Local clean-up:** the feature worktree, the merged branches and the `pre-cleanup` tag were removed, so owner action (2) above no longer applies.
- **Repository moved:** the owner transferred the repository to the DotNetLabX organisation the same day; commits, dates and the creation date are unchanged. The manual deploy workflow's trust entry in Azure still names the old owner and needs updating before that workflow is used again.
- **Complete test run after all of the above:** passed, 0 failed, 0 timed out.
- **Still the owner's:** making the repository public. Old commits can still be fetched from GitHub by their exact id until GitHub clears them; a request to GitHub Support removes them sooner. No secret scanner is installed on this machine; the history scan above covered the common credential formats.
