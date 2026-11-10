# F11 — Publish Cleanup — Codex code review

## Verdict: NO-GO

1. **HIGH — `docs/specs/F11-PublishCleanup/delivery/review.md:5`** — The generated list of deleted paths reintroduces the private harness abbreviation and private document names. Step 3’s private-project search finds this line with `--untracked`. **Suggested fix:** replace these paths with descriptions and redact generated delivery metadata before saving it.

2. **MEDIUM — `docs/specs/fokus/adhoc-SuiteInfrastructure/delivery/plan.md:277`** — The replacement changes an absolute solution command to `dotnet build SprintRituals.slnx`, while the same line says the working directory resets to the original Fokus repository. The command now targets the wrong checkout, and “absolute slnx path” is false. **Suggested fix:** explicitly enter the suite repository within each command invocation, or use an absolute-root placeholder.

3. **LOW — `docs/specs/F11-PublishCleanup/delivery/plan.md:83`** — The report-heading acceptance regex matches its own text in the untracked plan. Its required empty result is therefore impossible. **Suggested fix:** use a character class in that alternative, then rerun with `--untracked`.

4. **LOW — `docs/specs/adhoc-ReflektSprintOwnership/delivery/questions.md:41`** — Bulk replacements leave malformed prose, including “un-merged the original Reflekt project repo” and, at line 45, “existing the original Reflekt project client.” Related instances occur in:
   
   - `docs/specs/adhoc-ReflektSprintOwnership/delivery/lessons.md:7`
   - `docs/specs/adhoc-ReflektIdentityUnification/delivery/lessons.md:22`
   - `docs/proposals/nexus-format-gap.md:108`
   - `docs/specs/adhoc-Pass1-SkillRepair/delivery/implementation.md:5`
   
   **Suggested fix:** rewrite these replacement contexts as grammatical sentences while preserving their original meaning.

Carry-Over Findings dispositions:

| Row | Review result |
|---|---|
| ADR-023 still names the deleted strategy document | **Confirmed.** Accepted wording under the recorded coordinator ruling; it contains no broken file path. |
| Cross-ref cleanup remains Later | **Confirmed.** Backlog disposition remains owner-owned; historical records also remain outside the live-reference cleanup. |
| Removed settings included the deny list | **Confirmed fact; refuted as an implementation defect.** Removing the entire permissions block follows step 6. Local restoration belongs to close. |
| External repository paths use placeholders | **Confirmed and accepted** under the architect’s ruling. Findings 2 and 4 concern replacement contexts, not that notation. |

All 11 distinct grep acceptance searches ran with `--untracked`; their only hits are findings 1 and 3. Deletion, filename, ignore-rule, and settings checks passed. The exact Must-NOT-Change filename checks passed, and architecture edits stay on approved lines.

README commands, ports, settings, paths, and every step 7 item checked out. Deployment command blocks parse after filling placeholders; mappings are consistent; the licence matches the original. No additional private material was found in the broader scan.

Read-only review completed. The supplied handover pass was accepted; the suite was not rerun.
