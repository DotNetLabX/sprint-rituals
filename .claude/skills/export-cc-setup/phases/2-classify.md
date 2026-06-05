# Phase 2: Classify

Scan every inventoried file against the stack fingerprint and assign a classification. Present to user for approval.

## Steps

### 2.1 Scan file content

For each file in the inventory:

1. Read the file content fully.
2. For each substantive line (skip blank lines and lines that are only markdown formatting):
   - Test against every fingerprint pattern
   - Record each match: `{term, line_number, line_text}`
3. Calculate match percentage: `matched_lines / total_substantive_lines * 100`

For **skills**, also scan the `description:` frontmatter separately — a skill with technology names in its description is stack-specific regardless of body match percentage.

### 2.2 Apply classification rules

For each file, assign a classification using these rules in priority order:

**Portable** (include as-is):
- `docs/conventions/*` files are NEVER portable (always stack-specific) — skip this check for them
- Match % = 0 AND category is rule, convention, or hook or command or settings
- Match % = 0 AND category is skill AND description contains no technology names
- Match % = 0 AND category is agent or readme

**Stack-specific** (exclude):
- Category is `docs-convention` (always stack-specific by convention)
- Category is skill AND description names specific technologies
- Match % > LEAK_THRESHOLD (default 20%)

**Leaked** (block — remediate in source):
- 0 < match % <= LEAK_THRESHOLD
- Any pipeline file (agent, rule, convention, hook, command, readme, settings) with stack references
- Category is skill AND description is process-oriented BUT body has tech references

### 2.3 Special cases

**README.md:** Classify based on scan results. If it references stack-specific skill names or counts, it's leaked — the fix is to make the source README stack-agnostic.

**Agent files:** The highest-value portable files. Any fingerprint match is a leak. Remediation: move stack content to `docs/conventions/` and generalize the agent prose.

**settings.local.json:** Always exclude. Project-specific secrets and permissions.

**settings.json:** Classify based on scan results. If it has stack-specific permissions (e.g., `Bash(dotnet build)`), it's leaked — remediation: move those entries to `settings.local.json`.

**Commands (.claude/commands/):** Classify like any other file. Custom commands that don't mention stack terms are portable.

### 2.4 Remediation analysis (for leaked files)

For each file classified as **leaked**, analyze every match and propose a fix. See `playbooks/remediation-rules.md` for the full pattern catalog.

For each matched line, determine:
1. **What kind of content it is:** build command, framework name in prose, stack-specific example, permission entry, skill reference
2. **Where it should move:** the target file in the source project
3. **What the line should become:** the generic replacement that stays in the pipeline file

Produce a remediation report per leaked file:

```
LEAKED: .claude/agents/developer.md (3% — 4 lines)

  Line 45: "verify with `dotnet build && dotnet test`"
    Type: build command
    Move to: docs/conventions/project-rules.md § Build Verification
    Replace with: "verify the build passes (see docs/conventions/project-rules.md)"

  Line 112: "FastEndpoints endpoint classes own feature slices"
    Type: framework name in prose
    Replace with: "endpoint classes own feature slices"

  Line 230: references skill `create-feature` (excluded)
    Type: stack-specific skill reference
    Replace with: generic description or remove
```

### 2.5 Build classification table

Produce a table sorted by classification (leaked first, then stack-specific, then portable):

```
| File                           | Category  | Classification | Match % | Top matches           |
|--------------------------------|-----------|----------------|---------|-----------------------|
| agents/developer.md            | agent     | LEAKED         | 3%      | .NET, dotnet build    |
| skills/create-feature/SKILL.md | skill     | stack-specific | 85%     | FastEndpoints, EF Core|
| rules/agents-workflow.md       | rule      | portable       | 0%      | —                     |
```

### 2.6 CHECKPOINT 1

Present the classification table to the user via `AskUserQuestion`.

**If no leaked files exist:**

"All pipeline files scan clean (0%). Here's the classification:"

{table}

Options:
1. **"Approve and continue"** — proceed to Phase 3
2. **"I want to override some"** — reclassify specific files
3. **"Abort"** — stop the export

**If leaked files exist:**

"Export blocked — {N} files have stack leaks that must be fixed in the source project:"

{table}

Then show the remediation report for each leaked file (from step 2.4).

Options:
1. **"I'll fix these and re-run"** — stop export, user fixes source
2. **"Override — these are false positives"** — reclassify specific leaked files as portable (user asserts the matches are harmless)
3. **"Show me the matches for a file"** — drill into specific file's matches before deciding
4. **"Abort"** — stop the export

After overrides (if any), confirm the final classification. If any leaked files remain un-overridden, the export stops here.

### 2.7 Persist state

Write state to `{output-path}/_state/phase-2.yaml`:

```yaml
skill: export-cc-setup
version: 1.0.0
phase: 2
status: complete | blocked
timestamp: {ISO-8601}

classification_table:
  - path: "{relative-path}"
    category: "{category}"
    classification: "{portable|stack-specific|leaked}"
    match_pct: {number}
    matches:
      - term: "{term}"
        line: {number}
        text: "{line content}"
    remediation:                     # only for leaked files
      - line: {number}
        type: "{build-command|framework-name|skill-reference|permission-entry|example-block}"
        move_to: "{target file path}"
        replace_with: "{generic replacement text}"

checkpoint_overrides:
  - path: "{relative-path}"
    original: "{classification}"
    override: "{classification}"
    reason: "{user reason if given}"
```

On resume: if `phase-2.yaml` exists and `status: complete`, skip to Phase 3. If `status: blocked`, re-display the remediation report.

## Output

- Approved classification table (zero leaked files remaining)
- Remediation report (if leaks were found, for user to fix source)
- State persisted to disk

## Gate

CHECKPOINT 1 passed. Zero leaked files (all either fixed in source, overridden as false positives, or never leaked). Ready for Phase 3.
