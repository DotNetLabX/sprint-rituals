# F11 — Publish Cleanup — Review

## Step 2 — Risky steps

Risky steps: Step 3 (recheck: shared with Step 4), Step 4 (recheck: security), Step 5 (recheck: shared with Step 4), Step 6 (recheck: shared with Step 5), Step 7 (recheck: shared with Step 4), item src/Services/Fokus/Fokus.API/wwwroot/assets/LoginView-HBk0iTf4.js (security), item src/Services/Fokus/Fokus.API/wwwroot/assets/auth-DEoVD9np.js (security), item src/Services/Fokus/Fokus.API/wwwroot/assets/authStore-CueR_-tb.js (security) · Unplanned: 144 deleted files — the tool-state, old build-output and private-document files that plan steps 1 and 2 remove by folder (given as a count: some of their names carry a name this feature removes)

## Step 2 — Code Review

## Verdict: REQUEST CHANGES

Primary: Codex — NO-GO, persisted by the architect's session (the lane's coordinator)
**Model:** codex
Run record: the handover run, pass (no tied test for the changed files; the unit role ran) — cited, not re-run.

### [HIGH] The risky-steps line reintroduced a removed name
`docs/specs/F11-PublishCleanup/delivery/review.md:5` — the generated list of deleted paths carried the private harness abbreviation and private document names; the private-project search found it with `--untracked`. Fix: replace the paths with a description.

### [MEDIUM] A replaced build command targets the wrong checkout
`docs/specs/fokus/adhoc-SuiteInfrastructure/delivery/plan.md:277` — the absolute solution path became a bare solution name, while the same line says the working directory resets to the original Fokus repository; the command now reads as run in the wrong checkout and "absolute path" is no longer true. Fix: enter the suite repository inside the command, or use a repository-root placeholder.

### [LOW] The plan's report-heading search matches the plan itself
`docs/specs/F11-PublishCleanup/delivery/plan.md` step 2 — the last alternative of the search matches its own text in the untracked plan. Fix: a character class in that alternative.

### [LOW] Bulk replacements left ungrammatical phrases
`docs/specs/adhoc-ReflektSprintOwnership/delivery/questions.md:41` and `:45`, `docs/specs/adhoc-ReflektSprintOwnership/delivery/lessons.md:7`, `docs/specs/adhoc-ReflektIdentityUnification/delivery/lessons.md:22`, `docs/proposals/nexus-format-gap.md:108`, `docs/specs/adhoc-Pass1-SkillRepair/delivery/implementation.md:5` — a project name was dropped into a sentence that no longer reads. Fix: rewrite each as a grammatical sentence with its meaning kept.

Carry-over findings: all four confirmed as settled (the decisions register's strategy phrase; the backlog's cross-reference row; the removed safety list, restored locally at the close; the placeholder form for paths into other repos).

Checked and passed: all 11 acceptance searches with `--untracked` (only the two hits above, both in this feature's own records); deletions, ignore rules, settings; the Must-NOT-Change checks; the README against the code; the runbook commands; the licence. No other private material found.

*Status: COMPLETE — Codex, persisted by the architect's session, 2026-11-10*

## Step 2 — Fix list, cycle 1
Bar: every item (the light lane's one round) · always fixed: HIGH and up

1. [HIGH — contested HIGH/MEDIUM: it fails the final sweep] The risky-steps line reintroduced a removed name — `docs/specs/F11-PublishCleanup/delivery/review.md` (Codex, second reader) — fixed in round, not sent: the coordinator's own file, rewritten as a count; the private-project search is empty again
2. [MEDIUM] A replaced build command targets the wrong checkout — `docs/specs/fokus/adhoc-SuiteInfrastructure/delivery/plan.md:277` (Codex)
3. [MEDIUM — raised from the second reader's LOW: an employer identifier, which this feature exists to remove] A second personal settings folder is named with the employer's brand abbreviation — `docs/specs/adhoc-Pass1-SkillRepair/delivery/consumption-evaluation.md:85` (second reader)
4. [LOW] Bulk replacements left ungrammatical phrases — `docs/specs/adhoc-ReflektSprintOwnership/delivery/questions.md:41`, `:45`; `docs/specs/adhoc-ReflektSprintOwnership/delivery/lessons.md:7`; `docs/specs/adhoc-ReflektIdentityUnification/delivery/lessons.md:22`; `docs/proposals/nexus-format-gap.md:108`; `docs/specs/adhoc-Pass1-SkillRepair/delivery/implementation.md:5` (Codex)
5. [LOW] A sentence now starts with a lower-case phrase — `docs/architecture/decisions.md:15` (second reader)
6. [LOW] The plan's report-heading search matches the plan itself — `docs/specs/F11-PublishCleanup/delivery/plan.md` step 2 (Codex) — fixed in round, not sent: the coordinator's own file

Done check: check 1 PASS — no item.

Follow-up review: none due
