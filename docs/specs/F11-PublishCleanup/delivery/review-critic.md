# F11 — Publish Cleanup — plan review record

## Round 1 — 2026-11-10 — plan — primary (codex)

Header note: light lane, round 1 only. Codex ran at the user's own Codex effort (the repo sets none, so it was not lowered); the second reader ran one level down (medium). The primary's message is persisted as returned in `review-critic-codex.md` (verdict NO-GO, 12 findings). The second reader's message is persisted below with private tokens replaced by descriptions — a verbatim copy would put into this record the names this feature removes.

## Second reader — round 1 (sonnet)

Verdict: ACCEPT — no CRITICAL or HIGH; three MEDIUMs to fold before the build.

Coverage ledger (Read: full): all plan-time counts re-run and matching; secrets and build output (settings files, workflows, deploy scripts, tracked files by type and size) covered; personal and employer data beyond the searches partial (F2, F5); tracked names carrying tokens covered; dangling links from deletions covered; mirror comparison covered (F6); the deploy scripts' refusal of a missing target verified; main-checkout interaction covered (F4). Not run: build, tests; the tracked archive not opened.

- **F1 [MEDIUM, high]** The plan prints an employer identifier literally inside the machine-path pattern of step 4, so the close sweep would hit the plan itself.
- **F2 [MEDIUM, high on the misses]** The colleague search misses a diacritic spelling, one surname and seven first names that exist in the tree; all sit in files or sections step 2 deletes, so they go only if step 2 runs as written.
- **F3 [MEDIUM, high]** Step 2 says its deletions wait for the owner while the Decisions rows say decided and Open Questions says none.
- **F4 [LOW, medium]** The default branch's checkout holds uncommitted edits to the shared settings file and the backlog, both changed by the plan — a collision at merge.
- **F5 [LOW, medium]** Step 7's "searches stay empty" does not see untracked files; one temp-path form is outside the path search; the customer token has no site; the owner's GitHub handle appears in the runbook and two feature records, undecided.
- **F6 [LOW, high]** Departures from the mirrored plan with no row: no deploy-script change, the test declaration left untracked, plugin feedback kept.
- **F7 [LOW, high]** Step 5's site list names the decisions register, which holds no such link; the count of 4 is right.

Verified by the second reader: no app source, test, settings, workflow or seed file holds a private token; no credential, email, GUID or IP beyond the expected hosts; the 50-file inventory is exact; the baseline tag exists.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | The plan prints an employer name in two patterns | self-matching cleanup artifact | the close sweep fails on the plan, or the name ships in a record | untracked sweep hits two plan lines | no | patterns | steps 3, 4, 8 | codex 1, sonnet F1 |
| R1-02 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Inventory misses the bare Jira project prefix and the employer's release and feature-area names | search narrower than the data | they survive in a plugin feedback file and a product doc | lines read in both files | no | inventory | step 3 | codex 2 |
| R1-03 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Cloud search misses the old plan, the old resource group and the deployment identity name | search narrower than the data | they survive in the runbook, a proposal and a feature record | lines read in the three files | no | inventory | step 3 | codex 3 |
| R1-04 | codex, sonnet | MEDIUM | unranked — contested: MEDIUM / LOW, settled MEDIUM — two real path forms escape | Path search misses a temp form and a session-transcript form | search narrower than the data | two machine paths survive | lines read in two feature records | no | inventory | step 4 | codex 4, sonnet F5 |
| R1-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Live docs keep references that name no file | goal not covered by a step | "live docs point at real files" is not met | 22 unresolved references listed at plan time | no | links | step 5 | codex 5 |
| R1-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The ignore proof fails after an unstaged delete | acceptance cannot pass | step 1 reads red though correct | reproduced with and without the index flag | no | accept | step 1 | codex 6 |
| R1-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 6's acceptance proves one value gone, not the three keys | weak acceptance | the broad allow list could stay | acceptance read | no | accept | step 6 | codex 7 |
| R1-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | No limit line for the write into the main checkout's local settings | unbounded shared write | other local settings could be overwritten | step 8 read | no | settings | step 8 | codex 8 |
| R1-09 | codex, sonnet | MEDIUM | unranked — contested: MEDIUM / LOW, settled MEDIUM — the owner would not see the choice | Departures from the mirrored plan carry no row | undeclared departure | the owner never sees three skipped steps | mirror plan compared | no | decisions | Decisions | codex 9, sonnet F6 |
| R1-10 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | The feature-record invariant flags this feature's own records | invariant too tight | the pin reads red at close | invariant command read | no | invariants | Must-NOT-Change | codex 10 |
| R1-11 | sonnet, codex | MEDIUM | unranked — contested: MEDIUM / LOW, settled MEDIUM — a developer could stall or delete unapproved | Step 2 says pending, the rows say decided | contradictory status | one instruction is missing | step 2 and Decisions read | no | decisions | step 2 | sonnet F3, codex 11 |
| R1-12 | sonnet | MEDIUM | unranked — no ruler in the repo | Colleague search misses spellings and names in the tree | search narrower than the data | names survive if step 2 is not run in full | lines read in three files | no | inventory | steps 2, 3 | sonnet F2 |
| R1-13 | sonnet | LOW | unranked — no ruler in the repo | Uncommitted edits on the default branch touch two planned files | merge collision | a refused merge or a lost local switch | diff of the main checkout | no | close | steps 6, 8 | sonnet F4 |
| R1-14 | sonnet | LOW | unranked — no ruler in the repo | Step 7's check misses untracked files; the GitHub handle is undecided | weak acceptance | the new README goes unchecked until the close | acceptance read | no | accept | steps 3, 7 | sonnet F5 |
| R1-15 | sonnet | LOW | unranked — no ruler in the repo | Step 5's site list names a file with no such link | wrong site list | the developer chases a missing link | search re-run | no | links | step 5 | sonnet F7 |
| R1-16 | codex | LOW | unranked — no anchor fits; Codex severity LOW | The size line counts 9 files | count disputed | none for the build | header read | no | size | header | codex 12 |

### Fold — round 1 (architect, claude-fable-5-2, 2026-11-10)

| id | disposition | level after | re-grade (level · anchor · why) | where | reason |
|---|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | step 3 search 5, step 4 search | — |
| R1-02 | fixed-real | HIGH | — | step 3 rules and search 2 (plan time 18), Context | — |
| R1-03 | fixed-real | HIGH | — | step 3 rules and search 4 (plan time 85), Context | — |
| R1-04 | fixed-real | MEDIUM | — | step 4 rule and search (plan time 599) | generic system paths that name no person or project stay, by rule |
| R1-05 | fixed-real | MEDIUM | — | step 5 (22 references), step 8, Must-NOT-Change architecture row, Context | — |
| R1-06 | fixed-real | MEDIUM | — | step 1 accept | — |
| R1-07 | fixed-real | MEDIUM | — | step 6 accept | — |
| R1-08 | waived | MEDIUM | — | step 8 (adds missing keys only), Decisions row | the plan builds no check and no script; the one hand edit is now bounded in step 8 |
| R1-09 | fixed-real | MEDIUM | — | Decisions (one tagged row for the three skipped steps), Scope | — |
| R1-10 | fixed-real | MEDIUM | — | Must-NOT-Change feature-record row | — |
| R1-11 | fixed-real | MEDIUM | — | step 2 | — |
| R1-12 | fixed-real | MEDIUM | — | step 2 accept, step 3 search 3 (plan time 13) | — |
| R1-13 | fixed-real | LOW | — | step 8, Context | — |
| R1-14 | fixed-real | LOW | — | step 7 accept, step 3 search 6 and rule, Decisions | — |
| R1-15 | fixed-real | LOW | — | step 5 | — |
| R1-16 | waived | LOW | — | — | below the bar — the size rule counts no file under `docs/`, and deletions are not counted |

Reconciled: step 2 ↔ Decisions rows 1–2; step 3 searches ↔ Context counts ↔ step 5 accept ↔ step 7 accept ↔ step 8; step 6 ↔ step 8 ↔ the `Adds:` row; Must-NOT-Change rows ↔ steps 1, 2, 5; Scope ↔ step 8 (untracked settings files).

## Round 2 — 2026-11-10 — plan — primary (codex)

Header note: asked for by the owner at approval (the light lane's second round on the fixes), run beside the build. Delta read: the round-1 merge and fold, and the plan sections the fold touched. The primary's message is persisted in `review-critic-codex.md` under its second verdict (GO): all 16 round-1 findings closed, every touched section `collateral: none`, no finding at MEDIUM or above.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|

No findings.

### Fold — round 2 (architect, claude-fable-5-2, 2026-11-10)

| id | disposition | level after | re-grade (level · anchor · why) | where | reason |
|---|---|---|---|---|---|

Nothing to fold; the plan is unchanged by this round. Reconciled: none touched.
