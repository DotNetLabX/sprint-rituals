# Pipeline Craft Improvements — Lessons

## Developer Lessons

- **Edit tool rejects strings not verbatim in file.** When a file uses em-dashes (UTF-8 `—`) and you copy the string from the Read output, the match works. But when the file was written by Write and contains em-dashes, subsequent Edit calls on code fence blocks (triple backtick) can fail because the file tool normalizes whitespace differently. Workaround: use Write to rewrite the whole file when multiple edits are needed.
- **Phase 1 file reads do not persist as "in context" for Phase 2 Edit calls.** Files read during Phase 1 analysis are not remembered as "already read" when Phase 2 starts in a new context. Every Edit call requires the file to have been Read in the current Phase 2 session. Read the file again (even a small offset) before the first Edit to satisfy the tool's state requirement.
- **Structural verification should test lint check logic against actual data format.** The KB orphan-entry lint check shell script compared full paths against relative-path references in index.md, producing false positives. When implementing lint checks as shell commands for testing, align the comparison format to the actual file content format first.
- **Documentation-only plans have no build verification step.** When all changes are to .md files, the "build verification after each step" rule cannot apply. The testing strategy in the plan (structural verification: file existence, reference integrity, line count) replaces build verification for documentation plans. This is a valid exception and should be noted in the plan.

## Architect Lessons

- **Documentation-only plans can legitimately exceed line-count targets.** When a plan specifies both "extract content to shrink file" and "add new sections," the net result may exceed the target. The done check should verify that the extraction worked (old blocks removed, references in place) and that additions are non-padding — not fixate on the absolute line count. The developer's Key Decision explaining 367 vs 350 was valid and well-documented.
- **Pre-commitment predictions for documentation plans differ from code plans.** For code plans, typical gaps are missing error handling, wrong patterns, or skipped steps. For documentation plans, the likely gaps are: orphaned content (extracted but not removed from source), broken references (new file path doesn't match the reference), and inconsistent formatting across extracted files. Adjusting predictions to the work type improves done-check efficiency.

## Reviewer Lessons

- **Step insertions at the top of a numbered list require full renumbering.** When new steps are prepended to an existing numbered list, all subsequent steps must be renumbered. A duplicate step number (two steps both labeled "3.") is easy to miss because the content reads correctly — only the numbers are wrong. Pre-commitment prediction for any plan step that inserts into an existing numbered sequence: check for duplicate numbers.
- **improve-flow skill claims about which agent files have a section must be kept in sync with actual agent files.** The Anti-pattern Promotion section of improve-flow stated architect.md has an `## Anti-patterns` section, but that section was never created (plan did not require it). When a skill describes the set of files it targets, verify that description against the actual files. A false claim in a skill is a runtime error waiting to happen.
- **Documentation-only reviews still benefit from structural verification evidence.** Without a build step, the Evidence table should focus on file existence, reference integrity, and line count checks — not be left empty. Running targeted `Select-String` and `Get-ChildItem` commands provides the same confidence signal that a build output provides for code changes.

## Skill Gaps

- None identified — all steps had `Skill: None` as specified.
