# F11 — Publish Cleanup — Lessons

## Developer Lessons
- **Machine-path replacement: a bare repo root turned into prose doubles the wording when the sentence already names the repo before the path** ("reference project", "plugin source repo", "Fokus source repo" + the path). After a bulk replace, grep for `(project|repo|codebase) (the original|the reference codebase|this repo|the plugin source)` and fix those lines; also give command contexts (`git -C "…"`, `cd … &&`, code-block roots) placeholders, never prose. Cost here: a second cleanup pass over 8 lines in 10 files. **That grep was too narrow** — the review still found the class, and fix round 1 fixed 27 more lines: an article or adjective in front of the phrase ("the un-merged the original … repo", "the existing the original … client"), the phrase in front of a noun ("… project migrations"), a parenthetical repeating the name ("plugin source repo (the plugin source repo)"), "at this repo", a lower-case sentence start, and a bare command (`dotnet build X.slnx`) under a note saying the cwd is another checkout. What worked: a script that scans only the *added* lines of `git diff <baseline>` for those shapes, then reading every added line that holds a replacement phrase (about 110 here).
- **A cleanup that removes private tokens must check its own delivery record too.** I quoted one removed abbreviation in a step block and caught it only by running the step 3 search over `implementation.md`. Run the feature's token searches with `--untracked` over the delivery folder after writing each block, not only at the close.
- **`.claude/settings.json` cannot be edited in two stages:** the editor validates the file as Claude Code settings JSON after every Edit and refuses an intermediate invalid state. Remove keys by rewriting the whole file with one Write.

## Skill Gaps
### publish-cleanup
- **Kind:** missing
- **Searched for:** a recipe for making a private repo's tree publishable — build output and tool state, private-token replacement with character-class searches that never match themselves, machine-path rules, live-doc reference checking, personal settings leaving the shared settings file.
- **Why it would help:** this is the third run of the same cleanup (Fokus, Reflekt, here); each time the plan re-derives the rules and the developer rewrites the survey, replacement and reference-check scripts in the scratchpad.
- **References:** `docs/specs/F11-PublishCleanup/delivery/plan.md` (steps 1–7), `docs/specs/F11-PublishCleanup/delivery/implementation.md` (step 4 decisions, step 5 reference list)
- **Evidence:** [F11-PublishCleanup]

## Architect Lessons

- Done check (light lane, cleanup feature): the coordinator's own `## Step 2 — Risky steps` line in `review.md` lists deleted file paths verbatim, so it re-introduced a removed private token into a delivery artifact. On a scrub feature, the risk recheck's `Unplanned:` list needs the same masking rule as every other delivery artifact. Otherwise only the close's `--untracked` sweep catches it. Worth filing as plugin feedback for `risk-recheck.js`: it has no redaction hook.

## Reviewer Lessons

- Token searches catch only the spellings they list. On a scrub, also grep for sibling aliases of the private owner (a second tool-profile folder, a twin-plugin name) and for the deleted files' own names inside tool output pasted into delivery records.
