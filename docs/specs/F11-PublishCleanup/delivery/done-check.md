# F11 — Publish Cleanup — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-11-10 — after the build

**Model:** claude-opus-5-5

Lane: light. Every step was graded on its outcome and its acceptance evidence. Every acceptance command in the plan was run again in the worktree for this check.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Remove build output and tool state; fix `.gitignore` | Implemented | The file count prints 0. The `--no-index` ignore check prints nothing for all five probe paths, and `.claude/pipeline-state/` is ignored too. All six rules are lines of `.gitignore`. The build-only step run passed. |
| 2 — Remove the private documents | Implemented | The file-name search prints 0, the strategy file is gone and the name/report search prints nothing. The proposal was cut at the report heading. |
| 3 — Replace employer, people, cloud and private-project references | Implemented | All six searches print nothing. The example config uses placeholders. Every changed file under `docs/specs/` had a hit for one of the plan's searches at `pre-cleanup`. |
| 4 — Replace machine paths | Implemented | Both searches print nothing. Paths into the owner's other repos keep the file part behind a named placeholder: the developer flagged this reading and the architect ruled it correct (recorded in the step block). |
| 5 — Fix references in live docs | Implemented | The `suite-strategy` search over live docs prints nothing and step 3's search 5 stays empty. `implementation.md` lists all 22 references with their new targets: 21 plus the strategy link, and the difference is explained. Targets spot-checked: they exist. The four live docs with no search hit at baseline are all on step 5's list. |
| 6 — Keep personal settings out of the shared settings file | Implemented | The plan's `node -e` check exits 0. The removed block is recorded verbatim for the close. |
| 7 — Write the READMEs and the licence | Implemented | `Recommended IDE Setup` prints nothing. `README.md` and `LICENSE` are non-empty. The README holds all four required strings, a clone URL with a placeholder owner, the docs guide and a licence section. Steps 3–4 searches run with `--untracked` find nothing outside the delivery folder (see notes). The reviewer reads the remaining README items. |
| 8 — Final sweep, complete run, local settings | N/A (close-owned) | `Owner: close`. Not done yet, by design. |

**Must-NOT-Change (diff-pinned rows, re-run):** the `src`/`client` diff lists exactly `src/Services/Identity/CLAUDE.md` and the two client READMEs. The `deploy`/`.github` diff lists exactly `deploy/deploy-config.example.ps1`. Outside `docs/`, `src/`, `client/` and `deploy/`, only `.claude/settings.json`, `.gitignore` and `CLAUDE.md` changed. The architecture-meaning row is the reviewer's to read.

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (plan-only definition, no rule list)

**Skill log:** every step is `Skill: None`, `TDD: no`, so the all-`None` exemption applies. The developer's `## Skills Used` table agrees with the plan.

**Test-entry log** (this session, from the window start, both log homes): one `blocked` line, a corrected attempt, plus seven `script-unread` lines, which are disclosed misses (scratchpad scripts the gate could not read). No line without `blocked`, so there is no test slip.

**Note for the close (coordinator's artifact, not a developer item):** search 5 of step 3, run with `--untracked`, hits `review.md`. Its `## Step 2 — Risky steps` line lists the deleted file paths, and those paths carry the employer harness's abbreviation. Step 8 sweeps the delivery folder, so that line has to be reworded or masked before the commit. That follows the plan's rule that delivery artifacts never quote what the feature removes.

**Plan hygiene:** `## Decisions` and `## Decisions and why` are present and filled in.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-11-10*
