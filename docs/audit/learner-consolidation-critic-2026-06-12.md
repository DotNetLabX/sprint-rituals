# Pass 4/5 Learner Consolidation (2026-06-12) — Critic Review (Mode 3, Promotion Review)

## Verdict: GO (no CRITICAL, no HIGH; two MEDIUM, two LOW)

A small, faithful, well-evidenced consolidation. One genuine PATCH-worthy code promotion (the `cd /d` → "any cmd.exe idiom" generalization, at 3 occurrences) plus two tagging-only addenda that correctly claim "already covered at HEAD" with no new edit. Every coverage claim I checked holds against plugin source. No distortion, no over-promotion, no mis-routing, no artifact drift.

---

## (a) Faithfulness of the generalized csharp.md rule — PASS (LOW-1, confirmation only)

I diffed the old vs new Build Verification bullet. The generalization preserves **every** original technical claim:
- **bash misreads `/d`** — kept: "`/d` is a cmd flag, so `cd /d <path> && …` fails"
- **"too many arguments"** — kept verbatim
- **the command never runs** — kept: "the command never runs"
- **exit-1 masquerade** — kept: "the pipe's exit-code-1 masquerades as a real failure"
- **cwd reset between calls** — kept: "the Bash tool resets cwd between calls anyway"

The only softening is the exit-1 description changing from "masquerades as a **compiler** failure" to "masquerades as a **real** failure." This change is correct and *necessary* for the generalization — a failed git command is not a compiler failure. This is a faithful refinement, not a distortion.

**`git -C` form vs the M6 self-correction:** The M6 com-log entry (`adhoc-Pass4-IdentityExtraction/delivery/communication-log.md:100`) records the actual self-correction as `git -C "this repo" …`. The csharp.md generalization uses the generic placeholder `git -C "<private-repo>" …`. That is a correct generalization (placeholder for the repo path), consistent with the M6 entry. **No distortion.**

**Severity: LOW-1** (confirmation that faithfulness holds; no defect).

---

## (b) 3-occurrence recurrence justification, incl. the M6 "operator error" classification — PASS

**The 3 occurrences are real and distinct.** I grepped `cd /d | too many arguments | cmd-ism | cmd flag | cmd.exe` across `docs/specs` and confirmed exactly the three cited sources plus the new M6 com-log entry:
1. **Pass 0** — `adhoc-Pass0-Foundations/delivery/lessons.md:40` — developer, **build** ("Build invocation on Windows: avoid `cd /d`")
2. **Pass 3c-B** — `adhoc-Pass3c-B-DeliveryMetrics/delivery/lessons.md:20` — architect, **build** ("Build-command portability bite")
3. **Pass 4 M6** — `adhoc-Pass4-IdentityExtraction/delivery/communication-log.md:100` — team-lead, **git**

No double-counting, no fourth occurrence I'm missing. Against the learner's promotion bar (`learner.md:17` "2+ occurrences across features = promote"; `:47` "build-breaking… promote immediately"), 3 occurrences clears the bar comfortably.

**The M6 "operator error" classification question — judged a faithful refinement, NOT an over-reach.** The com-log explicitly classifies M6 as *"Operator error, not a plugin/tooling fault… Logged for completeness/honesty, not as a pipeline malfunction."* The key distinction: the learner did **not** invent a brand-new rule from an operator error. It **refined the scope of an existing, owner-approved rule** — the Build Verification rule introduced in nexus-dotnet 1.0.1 — using M6 as the 3rd data point of an *already-established class*. The rule's existence was already sanctioned; only its scope widened (build → any Bash-tool command), justified by a genuine recurrence in a context (git) the narrow build-only wording could not catch. Generalizing the scope of a sanctioned rule on a 3rd occurrence is exactly what the learner is supposed to do. **No over-reach.**

**Severity: none (PASS).**

---

## (c) Covered-at-HEAD claims for M4/M5 and R1–R7 — VERIFIED (one soft spot, MEDIUM-1)

I read every claimed HEAD location in plugin source. Results:

| Item | Claimed HEAD coverage | Verified? |
|---|---|---|
| **M4** (~6× partial inline results) | `agents-workflow.md:86` durable-artifact hard rule ("Your durable artifact is your primary deliverable, not your inline message") | YES — line 86 verbatim |
| **M4** (terse verdict relay) | `team-lead.md:50` "never relay a verdict you have not actually read" | YES — grep-confirmed at line 50 |
| **M5** (Codex actively-wrong result string) | `team-lead.md:207` Codex file-is-the-channel ("a bare chat ack… is never the result; a missing file = incomplete review") | YES |
| **R1** (critic wrote no file) | `agents-workflow.md:86` + TL recovery order (`team-lead.md:50`, re-spawn with file as primary deliverable) | YES |
| **R2** (`TaskOutput` "no task found") | TL recovery order names the artifact-first fallback | PARTIAL — see MEDIUM-1 |
| **R3/R4** (dev two-phase collapse + gate fabrication) | nexus 1.5.0 F16 remediation; `agents-workflow.md:87` (never author another's artifact) + `:88` (never spawn pipeline-role) | YES — both hard rules present |
| **R5** (Codex bare ack) | `team-lead.md:207` | YES — same rule as M5 |
| **R6** (stale `index.lock`) | malfunction-logging rule `team-lead.md:279/361` | YES — logging discipline (correctly framed as "logged," not "prevented") |
| **R7** (stale notification labels) | `team-lead.md:361` known platform caveat (track by agentId); reinforced at `:369` Resume | YES |

I also confirmed the version attribution: the nexus core CHANGELOG shows `b6be9c9 feat(nexus): 1.5.0 — F16 remediation` and `d463b31 feat(nexus): 1.4.0 — enforcement + relay package`, matching the addenda's "nexus 1.4.0–1.5.x" attribution.

**MEDIUM-1 — R2 coverage claim is softer than the addendum's "all addressed" implies.**
- **Source:** Pass5 R2 (`adhoc-Pass5-EventCorrectness/delivery/communication-log.md:43`); addendum (`lessons.md:10`) says R1–R7 "all addressed at plugin HEAD."
- **Issue:** R2 is a **platform tool defect** (`TaskOutput` returns "No task found" for completed agents), not a process gap the plugin can fix. What HEAD provides is a *workaround*, not a fix: `team-lead.md:50` makes the artifact the record of record ("the artifact, not `TaskOutput`, is what the result ultimately rests on") and adds `salvage-transcript` as a recovery step. So R2 is *mitigated/routed-around*, not "addressed."
- **Impact:** Low — the blanket "all addressed" is slightly generous for 1 of 7 items, but the operative conclusion (no further learner action needed) is correct: there is nothing to promote about a platform tool bug beyond the already-present don't-trust-`TaskOutput` discipline.
- **Suggestion:** Non-blocking. If tightening, the addendum could say "R2 mitigated (recovery order prefers the artifact; `TaskOutput` treated as unreliable for completed agents)" rather than grouping it under "addressed."

---

## (d) Version / CHANGELOG hygiene — PASS

- **plugin.json:** `1.0.2 → 1.0.3` — a **PATCH** bump. Correct: this is a doc/convention wording change with **no behavior change** (a convention file generalization, no skill logic altered). PATCH is the right SemVer level.
- **CHANGELOG [1.0.3] entry:** accurate against the actual diff. The entry says it "generalized the Build Verification `cd /d` rule from build commands to any command under the Bash tool (added the `git -C "…"` form)" and names the three occurrences (Pass 0 build, Pass 3c-B build, Pass 4 M6 git) — this matches the csharp.md diff exactly.
- **No drift:** csharp.md is a hand-maintained convention file with **no generated mirror** (unlike `agents/*.md`→`commands/*.md`), so there is no stale-mirror risk. The version bump + CHANGELOG entry + the edit all ship in the same working-tree change. No shipped-change-without-bump, no bump-without-entry.
- **No CHANGELOG contradiction:** the [1.0.3] entry explicitly references the [1.0.1] entry that introduced the original rule as the thing being generalized — consistent, not conflicting.

**Severity: none (PASS).**

---

## (e) Placement / omission judgment (no duplicate line in nexus core) — DEFENSIBLE (MEDIUM-2)

**MEDIUM-2 — the generalized rule lives in a file the M6 victim (team-lead) does not load.**
- **Source:** `csharp.md:3` states the file is "Inlined into the **developer, reviewer, and solo** agents"; M6 was committed by the **team-lead**.
- **Issue (verified):** I grepped `cmd.exe | cd /d | Git Bash | POSIX` over the entire nexus **core** (`plugins/nexus`) — zero hits except one unrelated path-shape comment in `guard.js:124`. The cmd-ism rule exists **only** in `nexus-dotnet/conventions/csharp.md`, which by its own header reaches developer/reviewer/solo — **not** the team-lead. So the agent that actually made the M6 mistake will not see this rule.
- **Why I do NOT escalate to HIGH (Realist Check):** The learner's stated rationale is sound and I verified its premise. The harness's **own Bash tool description** now states *"runs Git Bash (POSIX sh), not cmd.exe… Use Unix shell syntax: `/dev/null` not `NUL`… `$VAR` not `%VAR%`"* — this is visible in this very session's tool docs, it is a stronger always-present guard than any plugin convention file, and it reaches **every** agent including the team-lead. Adding a duplicate line to nexus core would (1) duplicate guidance the harness already ships, and (2) push stack-flavored detail (`.slnx`, `dotnet build`, `git -C`) toward the **stack-agnostic core**, brushing against ADR-1 (stack-specific detail belongs in the stack extension, not the core). The learner deliberately declined the duplicate **and recorded why** — that is correct judgment, not a gap.
- **Why it is still MEDIUM and not dismissed:** the rule's primary audience (developers running builds) is correctly served, and 2 of the 3 occurrences were build-context (developer + architect); the team-lead git case is the outlier. The placement matches the dominant case. The residual exposure (team-lead, non-build) is real but mitigated by the harness tool description.
- **Suggestion:** Accept as-is. Optionally, **only if** a 4th occurrence appears in a non-build, core-agent context, a future learner could add a single stack-agnostic sentence to the nexus core ("the Bash tool is POSIX sh — never cmd idioms like `cd /d`; the harness tool description is canonical"). Not warranted now.

---

## Additional gap-analysis confirmations (all clean)
- **No over-promotion:** the only code edit sits at 3 occurrences; M4/M5/R1–R7 were correctly **not** re-promoted — they are recurrence evidence for rules already at HEAD, tagged `[APPLIED]` with "no new edit."
- **No mis-routing:** the generalized rule is stack-flavored (`.slnx`, `dotnet build`, `git -C`) and correctly stays in the nexus-dotnet stack extension, not the core (ADR-1 respected).
- **No conflict/duplication:** the edit **replaces** the old build-only bullet in place (not append) — no two-rules-for-one-thing residue.
- **Tagging addenda are genuinely tagging-only:** the diffs add only the `> Learner addendum` blocks; no role sections rewritten, no file headers touched.

## LOW-2 — provenance dates checked
The Pass5 addendum header dates the R-log "logged 2026-06-09" while the addendum itself is 2026-06-12; the Pass4 addendum correctly notes M4–M6 were "logged post-consolidation" (matching com-log entry #56, the self-caught logging-gap audit). Both are internally consistent with their com-logs. No issue — flagged only to record that dates were verified.

---

## Self-Audit + Realist Check
Both MEDIUMs survive as MEDIUM (neither rises to HIGH): MEDIUM-1 is a wording nicety on 1 of 7 correctly-dispositioned items; MEDIUM-2 is mitigated by the always-present harness tool description, which I verified directly. No finding involves data loss, security, or silent incorrect user-facing output, so nothing is artificially held high. Zero CRITICAL, zero HIGH → **GO**, no ADVERSARIAL escalation triggered.

Reviewed:
- <plugin-repo>\plugins\nexus-dotnet\conventions\csharp.md
- <plugin-repo>\plugins\nexus-dotnet\CHANGELOG.md
- <plugin-repo>\plugins\nexus-dotnet\.claude-plugin\plugin.json
- docs\specs\adhoc-Pass4-IdentityExtraction\delivery\lessons.md
- docs\specs\adhoc-Pass5-EventCorrectness\delivery\lessons.md
- docs\specs\adhoc-Pass4-IdentityExtraction\delivery\communication-log.md
- docs\specs\adhoc-Pass5-EventCorrectness\delivery\communication-log.md
- docs\specs\adhoc-Pass0-Foundations\delivery\lessons.md
- docs\specs\adhoc-Pass3c-B-DeliveryMetrics\delivery\lessons.md
- <plugin-repo>\plugins\nexus\rules\agents-workflow.md (lines 70–104)
- <plugin-repo>\plugins\nexus\agents\team-lead.md (lines 40–52, 198–217, 270–289, 353–369)
- <plugin-repo>\plugins\nexus\agents\learner.md (promotion-bar lines 17/31/47)
- <plugin-repo>\plugins\nexus\CHANGELOG.md (1.4.0–1.7.1 version verification)

---

## Learner close-out (appended post-review, 2026-06-12)

Both MEDIUMs folded into the source tags: MEDIUM-1 wording fix applied to the Pass 5 addendum (R2 "mitigated, not fixed"); MEDIUM-2 accepted as-is with the 4th-occurrence revisit trigger recorded in the Pass 4 addendum. LOW-1/LOW-2 were confirmations, no action.

### Runtime observations from this consolidation run (1 occurrence each — TRACKED, below the promotion bar)

- **The critic subagent itself exhibited the R1/M4 class:** its task `.output` file was **0 bytes**, `TaskOutput` returned only a lifecycle closer, and the inline completion result carried the verdict but not the findings. Recovery succeeded via the real transcript JSONL at `~\.claude\projects\{project}\{session}\subagents\agent-{id}.jsonl` (the Temp `.output` path the spawn result names is not where the content lives).
- **`salvage-transcript` (nexus 1.4.1) default selection returned a 182-char lifecycle closer instead of the 13 KB findings block** present earlier in the same transcript — the exact failure its documented "longest-recent sees past verbose lifecycle closers" selection is meant to avoid. Repro: transcript with ~10 stacked closers after the substantive block; the script picked the last text, not the longest-recent. Plugin-bound bug candidate for the maintainer; promote/fix on recurrence or at next plugin maintenance pass.