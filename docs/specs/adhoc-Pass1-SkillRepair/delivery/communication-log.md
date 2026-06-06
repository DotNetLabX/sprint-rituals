# Pass 1 — SkillRepair — Communication Log

Audit trail for the `adhoc-Pass1-SkillRepair` pipeline run. Hub-and-spoke: all messages route through the team lead.

| # | From → To | Phase / Cycle | Message | Problem / Note |
|---|-----------|---------------|---------|----------------|
| 1 | user → team-lead | launch | "Move forward with refactoring; next is adhoc-Pass1-SkillRepair." | Team mode chosen: **Standard + Codex**. Branch: `main`. |
| 2 | team-lead → architect | architect:analyze | `Analyze adhoc-Pass1-SkillRepair.` | Phase 1 spawn (foreground). Gate set `architect:analyze`. |
| 3 | architect → team-lead | architect:analyze | Idle — 4 blockers raised in `questions.md` (Q1 landing location, Q2 domain-service shape, Q3 scope, Q4 acceptance bar). | Q1 reverses plugin-canonical decision; Q2 is the user's interactive design checkpoint. Routed to **user** (no spec/PO; genuine decisions). |
| 4 | user → team-lead | answers | Q1: skills live in editable plugin source repo **`<plugin-repo>`** (not the read-only cache) → fix there. Q3: **5 core only**. Q4: **proposed bar + dry-run gate**. Q2: "I don't get this — we need to discuss it." | Verified: all 5 core skills under `plugins/nexus-dotnet/skills/`. Operating model = skill edits in plugin repo, pipeline artifacts in sprint-rituals. **Q2 still open — discussing with user before architect Phase 2.** |
| 5 | team-lead ↔ user | Q2 discussion | Q2 resolved interactively (see Resolved Decisions below). Review mode chosen: **Critic**. | Q2 interactive design checkpoint closed. All blockers resolved. Gate → `architect:plan`. |
| 6 | team-lead → architect | architect:plan | `Write the plan. Review mode: critic.` + full Q1–Q4 answers. | Phase 2 spawn (resumed). Plan → critic review next. Awaiting completion. |
| 7 | architect → team-lead | plan done | Plan written (`plan.md`). Q1–Q4 marked Resolved. Critic ran **in-context** (spawn unavailable in architect context): found 1 MEDIUM (ADR-006 no-DTO-into-domain — input side of boundary) + 4 LOW, all applied. Architect flags in-context review is weaker; recommends fresh-context re-review. | Plan is two-repo (skill edits in `claude-plugins\nexus`, artifacts here). **In-context critic ≠ independent — not accepted as the gate.** TL spawning fresh `nexus:critic` + Codex for proper author≠reviewer separation (honors critic mode + Standard+Codex). |
| 8 | team-lead → critic + Codex | plan review | Independent review of `plan.md` against ADRs + resolved Q1–Q4. | Fresh-context critic (author≠reviewer) + Codex second opinion on skill designs. Parallel. Awaiting verdicts. |
| 9 | critic → team-lead | plan review | **GO** — 3 MEDIUM + 4 LOW, no CRITICAL/HIGH (`review.md`). Every factual claim independently verified against source; ADR-006 confirmed genuinely encoded. MEDIUMs: (1) FastEndpoints version mismatch — solution on **8.1.0**, Step 5 anchors on 7.x; (2) `improve-skills` housekeeping (backlog/CHANGELOG/README) unrouted across the two repos; (3) Step 6 `--plugin-dir` points at nexus-dotnet only, may not load `nexus` core dependency. | GO = plan not wrong; MEDIUMs are fold-before-implement. Holding auto-approve to fold MEDIUMs (esp. #1, a real skill-correctness risk) + any Codex findings into the plan first. |
| 10 | codex-review → team-lead | plan review | Returned terse "Done." — full report requested via resume. | Awaiting Codex findings to batch with critic MEDIUMs for a single architect revision. |
| 11 | codex-review → team-lead | plan review | **NO-GO** — 6 HIGH + 3 MEDIUM (report recovered from `tasks/bhh0fauif.output`). Code-grounded: read live `BugRatioService.cs`, `GraphQLXrayClient.cs`, `SyncSprintsEndpoint.cs`, `UpdateStatusEndpoint.cs`. HIGHs: (1) output boundary rule internally inconsistent / "no DTO twins" weakens API boundary; (2) input rule too strict — excludes intrinsic computation primitives (windowSize etc.); (3) repo escape-hatch under-specified, leaks IQueryable/persistence into Domain; (4) Step 1 analytics scrub incomplete — old API framing remains; (6) Step 2 domain-patterns leaves ADR-004-incompatible MediatR/Application content; (8) Step 5 SendAsync detection not operational (raw grep finds only HttpClient+SignalR here). MEDIUMs: (5) no domain result-type replacement named; (7) CPM grep misses child `<Version>`/`VersionOverride`; (9) Step 3 vs Step 7 cross-ref mismatch. | Cross-model caught real issues + a meta-concern: nexus-dotnet skills are SHARED/multi-variant (cqrs-patterns=MediatR, create-feature=multi-framework), so repairs must not hard-code SprintRituals' stack. Two items need USER decision; rest → architect revision. |
| 12 | team-lead ↔ user | review triage | User decisions: **(1) skills = General/multi-variant** (SprintRituals ADRs are one documented variant; don't hard-code ADR-004/011 as absolutes). **(2) API boundary = endpoint ALWAYS owns a response DTO + map, even 1:1** (supersedes earlier directive (d) "no DTO twins"). | Consolidated verdict = needs-revision. 7 Codex/critic items → architect; 2 keystones decided by user. Gate stays `architect:plan`. |
| 13 | team-lead → architect | architect:plan (revise) | Consolidated revision: 2 user decisions + 6 Codex HIGH/MED + 3 critic MED. | Single revision round. Re-review (critic + Codex) after. |
| 14 | architect → team-lead | plan revised | Revised `plan.md` delivered. All D-A/D-B/Codex(HIGH #1-4,#6,#8; MED #5,#7,#9)/critic(C-1,C-2,C-3; LOW-4..7) folded; honest "Revision notes" section explains 2 qualifications (CPM grep = generality req, no-op here; #6 annotate-not-scrub per D-A). No finding dropped. | TL spot-check: coverage looks complete. Not accepting on say-so — re-running both reviewers to confirm HIGHs actually resolved + no new inconsistency. |
| 15 | team-lead → critic + Codex | re-review | Re-review REVISED plan: confirm prior findings resolved, no new issues. Codex to re-verify its 9 findings RESOLVED/PARTIAL/NOT. | Resumed both reviewer agents (carry prior-finding context). Codex to also write report to `delivery/codex-rereview.md` (avoids relay truncation). Awaiting verdicts. |
| 16 | critic → team-lead | re-review | **GO (unambiguous)** — `review.md` updated. All 3 critic MEDIUMs + all 9 Codex findings + D-A + D-B folded and re-verified against source (re-grepped live file:lines). No new MEDIUM+ contradiction. One LOW (ADR-010 vs D-B attribution) — no change needed. | Verdict validated (no open CRITICAL/HIGH). Holding approval for Codex re-verification (its NO-GO was the gate). |
| 17 | codex-review → team-lead | re-review | **NO-GO** — all 9 prior findings RESOLVED, but 2 NEW from the revision (`codex-rereview.md`). **HIGH:** Step 3 pt1/pt3 conflates patterns — `ArticleAccessChecker` (reference §6) is an Application-layer service using `DbContext`, presented as a "full-stack variant of domain-service" yet contradicting pt3's read-port purity; the muddle propagates via Step 1's specialization. **MED:** `improve-skills` is scoped to native `.claude/skills/` and disclaims non-native formats — no adaptation note for plugin-repo authoring. | Both legitimate (cross-model catch). Neither reverses a user decision / changes scope → targeted architect fix, no user involvement. D-A preserved by reframing full-stack as a *contrast* (distinct Application-service pattern), not a domain-service variant. |
| 18 | team-lead → architect | architect:plan (touch-up) | Fix Codex HIGH (de-conflate domain-service vs Application-service; full-stack = contrast/see-also) + MED (improve-skills plugin-repo adaptation note). | Targeted, localized to Step 3 + disposition. Re-verify with Codex after. |
| 19 | architect → team-lead | plan touched-up | v3 delivered. FIX 1: Step 3 pt1 scopes domain-service to single Domain-layer pure pattern; ArticleAccessChecker = contrast/see-also (NOT variant), cross-ref cqrs-patterns; pt6 DI + ADR-005 row + Accept updated; pt3 purity intact. FIX 2: improve-skills format/location adaptation note added. | TL read the diff — both fixes correct & localized. Dispatching Codex to close its own 2 findings; skipping full critic re-run (localized, critic already GO). |
| 20 | team-lead → Codex | re-verify (FIX 1,2) | Confirm HIGH (de-conflation) + MED (improve-skills note) resolved in v3. | Final gate. If GO → approve + commit plan + start developer. |
| 21 | Codex → team-lead | re-verify (FIX 1,2) | **GO** — FIX 1 RESOLVED, FIX 2 RESOLVED, no new issues (`codex-rereview.md` "Touch-up confirmation"). | Both reviewers now GO on v3. Verdict validated (no open CRITICAL/HIGH). **Plan APPROVED.** |
| 22 | team-lead | plan approved | Committed `feat(adhoc-Pass1-SkillRepair): add implementation plan` (a515e60, 6 delivery files; markers excluded). | Auto-commit per Commit Protocol. On `main` (consistent w/ Pass 0). |
| 23 | team-lead → developer | developer:analyze | `Analyze adhoc-Pass1-SkillRepair.` | Phase 1 spawn (foreground). Gate set `developer:analyze`. Developer edits land in PLUGIN repo `<plugin-repo>`. |
| 24 | developer → team-lead | implemented | `implementation.md` ready. **COLLAPSED two-phase** — analyzed (0 questions) AND implemented in one run. Created 3 skills (domain-service, central-package-management, framework-currency), repaired 2 (analytics-computation-service full reframe, domain-patterns additive), 2 CHANGELOGs, skill-backlog.md. Self-audit (Step 7) all pass; "Deviations: None". | **LESSON:** `developer:analyze` gate watches sprint-rituals tree, NOT the plugin repo → cross-repo blind spot let the collapse through. No question-checkpoint actually skipped (0 questions). **Provenance check:** `docs/architecture/README.md` (ADR-10 spawn-mode) mtime 12:52 ≪ dev run 13:37+ → NOT developer's work (pre-existing/earlier overreach); excluded from Pass 1 commit, surfaced to user. `.claude/`/`.omc/`/`docs/audit/` = runtime pollution, exclude. |
| 25 | team-lead → architect | done-check | `Step 1 done check.` — verify implementation.md vs plan (7 steps); spot-check grep accepts; scope to Pass 1 skill files only. | Independent verification (architect authored plan, developer wrote code → author≠reviewer holds). Record collapse + cross-repo-gate lesson. |
| 26 | architect → team-lead | done-check | **PASS** (`done-check.md`). All 7 steps covered; every grep-accept re-run against real files & holds; cross-refs resolve; pre-commitment predictions all "did not occur". Scope exclusions (README ADR-10 + pollution) noted, not failed. | Verdict validated (PASS, no step Missing). → Step 2 code review. |
| 27 | team-lead → reviewer + Codex | Step 2 review | Review the 5 Pass 1 SKILL.md bodies in the PLUGIN repo diff — quality, example correctness (compile vs `Blocks.Domain`), Pass 2/3 usability. Scope to Pass 1 skills; ignore README/pollution. | reviewer → `review.md` (Step 2 section); Codex → `codex-review-impl.md`. Parallel. Max 3 fix cycles. |
| 29 | codex-review → team-lead | Step 2 review | **NO-GO** (`codex-review-impl.md`) — 7 HIGH + 4 MED + 1 LOW, code-grounded against live src. Root cause: skills present target-state code as live "SprintRituals worked examples"; + factual errors vs real BuildingBlocks (`IDomainEvent`, identity model, interceptor/publisher variants, `SingleValueObject` equality); + Mapster assumed but absent from the stack. See Ledger §E. | Confirms reviewer RV-1 (IDomainEvent). Adds the live-vs-target labeling issue + Mapster stack gap (⚠️ user decision). Batching ALL Step 2 findings (RV-1..7 + CX-1..16,X1,X2) → developer fix Cycle 1/3. |
| 30 | team-lead ↔ user | decision | TL verified Mapster absent (not in CPM/csproj, zero `.Adapt<>` usage; endpoints map manually). User decision: **ADOPT Mapster (keep ADR-010)** — skills prescribe `.Adapt<>()` labelled target-state; Pass 2/3 adds Mapster to CPM + converts manual maps. | ADR-010/CLAUDE.md stand. **Flag:** "add Mapster to CPM" = Pass 2/3 prerequisite. |
| 31 | team-lead → developer | fix Cycle 1/3 | Consolidated fix: RV-1..7 + CX-1..16/X1/X2, grouped by 10 themes + Mapster decision. Verify every "live" example against real src; box other-stack variants reference-only (D-A); use real config VOs; relabel target-state. | Single cycle. Re-review (reviewer + Codex) after. Deliverables in plugin repo. |
| 32 | developer → team-lead | fixes done | Cycle 1 fixes applied; `implementation.md` updated. (TL did NOT read it — lean protocol; reviewers verify.) | TL now lean: trust checkpoint, grep verdicts only. |
| 33 | team-lead → reviewer + Codex | re-review C1 | Re-verify your own Step-2 findings (RV-1..7 / CX-1..16/X1/X2) against the updated 5 skill files — RESOLVED/PARTIAL/NOT + regressions. Pointers only, findings live in `review.md` §STEP2 + `codex-review-impl.md`. | Lean dispatch (no inlined findings). reviewer → APPROVE/RC; Codex → GO/NO-GO. |
| 34 | reviewer → team-lead | re-review C1 | **APPROVED** (`review.md` §"STEP 2 — RE-REVIEW (Cycle 1/3)") — RV-1..7 all RESOLVED, no regressions. | Verdict valid (no open HIGH). Holding close for Codex re-verify (its NO-GO was the gate). |
| 35 | codex → team-lead | re-verify C1 | **NO-GO** — 17/18 RESOLVED; **CX-13 PARTIAL** (`codex-review-impl.md` §Re-verification): `domain-patterns:86-94,116-124` — `DomainEvent<TAction>` variant boxed reference-only ✓, but main behavior example still uses nonexistent `IAction`/`AssigneeChanged` as if live. | Reviewer APPROVED; Codex blocks on CX-13 only. One residual → developer Cycle 2/3 (targeted). |
| 36 | team-lead → developer | fix Cycle 2/3 | Fix CX-13 only (domain-patterns main behavior example uses nonexistent `IAction`/`AssigneeChanged`). Pointer to `codex-review-impl.md`. | Lean dispatch. Codex re-verify CX-13 after (reviewer already APPROVED). |
| 37 | developer → team-lead | fix done C2 | CX-13 fix applied; `implementation.md` updated. (TL didn't read — lean.) | → Codex re-verify CX-13 only. |
| 38 | team-lead → Codex | re-verify C2 | Confirm CX-13 resolved in `domain-patterns`. | If GO → close Step 2 → shutdown (summary + commits). |
| 39 | codex → team-lead | re-verify C2 | **GO** (`codex-review-impl.md` §"Re-verification (Cycle 2)") — CX-13 **RESOLVED**: `domain-patterns:88` labels `Assignee`/`IAction`/`AssigneeChanged` as illustrative pseudocode; line 100 redirects to real `UserCreated`/`PersonCreated` records. 18/18. | **Step 2 closed** — reviewer APPROVED + Codex GO, both valid. → shutdown. |
| 40 | team-lead (close) | shutdown | Wrote `summary.md`; backlog Pass 1 → **Done**, Pass 2 → Ready (+ Mapster-to-CPM prereq). | Two-repo commit next. |
| 41 | team-lead (close) | commit | Plugin repo: 5 skills + 2 CHANGELOG + skill-backlog.md (excl. README ADR-10 WIP + `.claude/`/`.omc/`/`docs/audit/`). Sprint-rituals: delivery artifacts + backlog (excl. `.claude/` runtime state). | Pipeline complete. |
| 28 | reviewer → team-lead | Step 2 review | **REQUEST CHANGES** (Cycle 1/3) — 1 HIGH + 3 MED + 3 LOW. HIGH: `domain-patterns:107` `IDomainEvent : INotification, IEvent` is wrong — real `Blocks.Domain/Events/IDomainEvent.cs` = `IDomainEvent : IEvent` (no MediatR/INotification) → copy = non-compiling. MED: `domain-service:82` `_history` primary-ctor param style; `analytics:76` `SparklinePoint` undefined anchor; `domain-patterns:12-16` audit-field types omitted (`CreatedById string?` not `int?`). LOW×3: dup naming caveat; CPM grep `src/**` shell-portability; framework-currency 2-arg SendAsync footnote. Strong positives: de-conflation clean, SMELL gone, D-A/D-B correct. | Verdict valid (HIGH present). Batching with Codex impl findings → single developer fix Cycle 1/3. |

---

## Resolved Decisions (authoritative — relayed to architect for Phase 2)

**Q1 — Skill landing / operating model:** Skills are fixed in the **plugin source repo `<plugin-repo>`**, under `plugins/nexus-dotnet/skills/`. Pipeline artifacts (plan/implementation/review/lessons/summary) stay in `sprint-rituals\docs\specs\adhoc-Pass1-SkillRepair\delivery\`. Two repos, two commit targets. Plan must include a cache-rebuild/sync step so the installed plugin reflects the edits (needed for the Q4 dry-run scan).

**Q2 — `domain-service` skill shape (interactive checkpoint, resolved):**
- **(a) Relationship:** Coexist. `analytics-computation-service` is rewritten as a **specialization** of `domain-service` (references the base, returns domain types, adds only analytics-specific guidance: sparklines, rolling averages, delta/direction/polarity).
- **(b) Naming:** **No `Service`/`DomainService` suffix.** Name the class by what it does — `SprintSummaryCalculator`, `CycleTimeAnalyzer`, `BugRatioScorer`, etc. The skill states this as an explicit rule with a "don't reach for `Service`" callout. (Rule lives in the `domain-service` skill; project-wide convention is a possible later add, out of Pass 1 scope.)
- **(c) Dependencies:** **Pure by default** — the endpoint handler loads aggregates via repos and passes them into the domain class; domain stays DB-free/testable. **Escape hatch:** inject repo interfaces into the domain *only* for performance when the domain knows better what/how to load, and only with that justification.
- **(d) Return boundary:** Domain types/VOs out; endpoint maps to the response DTO — **but no gratuitous DTO twins.** Introduce a distinct response DTO only when the wire shape diverges from the domain type.

**Q3 — Scope:** 5 core only — repair `analytics-computation-service` + `domain-patterns`; create `domain-service` + `central-package-management` + `framework-currency`. `create-feature`/`cqrs-patterns`/`service-registration` untouched.

**Q4 — Acceptance bar:** Each skill cites its governing ADR(s); passes anti-pattern/over-specification checks; any embedded code example compiles against this solution's BuildingBlocks; "accepted not proven until Passes 2/3 consume it" note. **Plus** a dry-run of `create-implementation-plan`'s skill-inventory scan against the new project-local/plugin skills.

**Review mode:** Critic.

---

## Problems Ledger (every issue raised this run, with status)

Complete audit of all problems found across the Pass 1 pipeline. Status: ✅ resolved · 🔧 open (in fix cycle) · ⏳ pending review · ⚠️ flagged to user.

### A. Plan review round 1 — Codex (code-grounded) NO-GO: 6 HIGH + 3 MED
| ID | Sev | Problem | Status |
|----|-----|---------|--------|
| PC-1 | HIGH | Step 3 output rule internally inconsistent — "no DTO twins" invites returning domain types from endpoints, weakens API boundary | ✅ resolved by D-B (endpoint always owns DTO) |
| PC-2 | HIGH | Step 3 input rule too strict — "domain types/VOs only" wrongly bans intrinsic primitives (`windowSize`, `anchorIndex`) | ✅ resolved (Step 3 pt5 primitives carve-out) |
| PC-3 | HIGH | Repo escape-hatch under-specified — leaks `IRepository`/`IQueryable`/persistence into Domain | ✅ resolved (narrow domain-owned read port; forbids concrete repos/IQueryable) |
| PC-4 | HIGH | Step 1 analytics scrub incomplete — old API/Service framing left in description/when-to-use/table | ✅ resolved (whole-skill reframe + grep accept) |
| PC-5 | MED | No domain result-type replacement named for in-service response trees | ✅ resolved (`BugRatioResult` domain record) |
| PC-6 | HIGH | Step 2 domain-patterns leaves ADR-004-incompatible MediatR/Application content | ✅ resolved (annotate full/light variant, not scrub) |
| PC-7 | MED | CPM verification grep misses child `<Version>` / `VersionOverride=` | ✅ resolved (three-form grep) |
| PC-8 | HIGH | `SendAsync` detection not operational — raw grep hits only HttpClient/SignalR here | ✅ resolved (two-stage detection) |
| PC-9 | MED | Step 3 ↔ Step 7 cross-ref mismatch | ✅ resolved (refs added to Step 3) |

### B. Plan review round 1 — Critic: 3 MED + 4 LOW
| ID | Sev | Problem | Status |
|----|-----|---------|--------|
| CR-C1 | MED | FastEndpoints version drift — solution on 8.1.0, plan anchored 7.x | ✅ resolved (8.1.0 verify-against-installed) |
| CR-C2 | MED | `improve-skills` housekeeping (backlog/CHANGELOG/README) unrouted across two repos | ✅ resolved (routed to plugin repo) |
| CR-C3 | MED | Step 6 `--plugin-dir` may not load `nexus` core dependency | ✅ resolved (verify-invocation; two-dir command) |
| CR-L4 | LOW | no-`Service`-suffix rule unscoped vs `extract-feature-service` | ✅ folded (scoped to domain services) |
| CR-L5 | LOW | `scripts/` enumeration omits `skill-classification.md` | ✅ folded |
| CR-L6 | LOW | Step 1 line-number drift | ✅ folded (grep accepts authoritative) |
| CR-L7 | LOW | `improve-skills` blind `.claude/skills/` glob | ✅ folded (gap pre-verified note) |

### C. Plan re-review round 2 — Codex: 2 NEW (introduced by the revision)
| ID | Sev | Problem | Status |
|----|-----|---------|--------|
| TU-1 | HIGH | Step 3 conflates `domain-service` with Application-service (`ArticleAccessChecker` uses DbContext) — contradicts pt3 purity | ✅ resolved (de-conflated; contrast/see-also, not a variant) |
| TU-2 | MED | `improve-skills` scoped to native `.claude/skills/`, no plugin-repo adaptation note | ✅ resolved (format/location adaptation note) |

### D. Implementation review (Step 2) — Reviewer REQUEST CHANGES: 1 HIGH + 3 MED + 3 LOW
| ID | Sev | File | Problem | Status |
|----|-----|------|---------|--------|
| RV-1 | **HIGH** | `domain-patterns:107` | `IDomainEvent : INotification, IEvent` is wrong — real `Blocks.Domain/Events/IDomainEvent.cs` = `IDomainEvent : IEvent` (no MediatR); copy → non-compiling | 🔧 open (fix cycle 1) |
| RV-2 | MED | `domain-service:82` | `_history` as primary-ctor param — misleading (param ≠ field) | 🔧 open |
| RV-3 | MED | `analytics:76` | `SparklinePoint` used with no definition anchor (which layer?) | 🔧 open |
| RV-4 | MED | `domain-patterns:12-16` | audit-field types omitted — `CreatedById` is `string?` not `int?` | 🔧 open |
| RV-5 | LOW | `domain-service:40-49` | "scoped to domain services" caveat repeated | 🔧 open |
| RV-6 | LOW | `central-package-management:83-103` | grep `src/**/*.csproj` shell-portability (globstar) | 🔧 open |
| RV-7 | LOW | `framework-currency:47` | 2-arg `SendAsync(body, ct)` overload not mapped | 🔧 open |

### E. Implementation review (Step 2) — Codex (code-grounded) NO-GO: 7 HIGH + 4 MED + 1 LOW
Root cause behind most HIGHs: skills present **target-state** code (what Pass 2/3 will build) as **live "SprintRituals worked examples"**, + factual errors vs real BuildingBlocks. Full report: `codex-review-impl.md`.
| ID | Sev | File:line | Problem | Status |
|----|-----|-----------|---------|--------|
| CX-1 | HIGH | domain-service:36 | Worked example `Fokus.Domain/Analytics/SprintSummaryCalculator.cs` does NOT exist (live = `Fokus.API/Features/Analytics/SprintSummaryService.cs`) | 🔧 open |
| CX-2 | HIGH | domain-service:125-135 | Endpoint example hard-codes Mapster `.Adapt<>()` — **Mapster not in `Directory.Packages.props`**, live endpoints map manually → won't build | 🔧 open (⚠️ user decision) |
| CX-3 | MED | domain-service:74 | Read-port returns `IReadOnlyList<SprintSummary>` — no live domain `SprintSummary` type | 🔧 open |
| CX-4 | MED | domain-service:129 | Example uses `GetBugRatioQuery`/`GetBugRatioResponse`; live = `GetBugRatioRequest`/`BugRatioResponse` | 🔧 open |
| CX-5 | HIGH | analytics:34 | Same nonexistent `SprintSummaryCalculator.cs` worked-example as CX-1 | 🔧 open |
| CX-6 | HIGH | analytics:91-96 | Mapster again — won't build (see CX-2) | 🔧 open |
| CX-7 | MED | analytics:42-47 | Naming table STILL lists Service-suffix names (`CycleTimeService`, `BugRatioService`, `WorkloadService`, `SprintSummaryDomainService`) — SMELL not fully removed | 🔧 open |
| CX-8 | MED | central-package-management:72-74 | Migration recipe step doesn't cover child `<Version>` / `VersionOverride=` (only the checks at 78-105 do) | 🔧 open |
| CX-9 | LOW | framework-currency:65-68 | Stage 1 "unqualified" wording inaccurate (pattern hits qualified FPs; stage 2 filters) — reword "candidate"; fix receiver names | 🔧 open |
| CX-10 | HIGH | domain-patterns:19 | Identity variant wrong — live `User` is `Entity<int>`, NOT `IdentityUser<int>`, no `IAggregateRoot` (CLAUDE.md locks "no ASP.NET Identity") | 🔧 open |
| CX-11 | HIGH | domain-patterns:57-77 | Worked example invents `BugRatioConfig`/`XrayConfig`/`CycleTimeConfig` (don't exist); `AppSettings` is plain class. **Live VOs already exist**: `HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig` (+`OwnsOne`) | 🔧 open |
| CX-12 | HIGH | domain-patterns:107 | `IDomainEvent : INotification, IEvent` wrong — real = `IDomainEvent : IEvent` (**= reviewer RV-1, both agree**) | 🔧 open |
| CX-13 | HIGH | domain-patterns:111-116 | `DomainEvent<TAction>`/`IAction` variant not grounded — no such types exist anywhere in src | 🔧 open |
| CX-14 | HIGH | domain-patterns:126-131 | `TransactionalDispatchDomainEventsInterceptor` doesn't exist; live = `DispatchDomainEventsInterceptor` | 🔧 open |
| CX-15 | HIGH | domain-patterns:145-147 | MediatR publisher variant (`Blocks.MediatR`, `IMediator.Publish`) not in src; only `Blocks.FastEndpoints/DomainEventPublisher` exists | 🔧 open (D-A: box as reference-only variant) |
| CX-16 | MED | domain-patterns:36 | `SingleValueObject<T>` equality semantics described wrong vs source | 🔧 open |
| CX-X1 | HIGH | cross-skill | Target-state/nonexistent artifacts labeled as live "SprintRituals worked examples" — blurs live vs ADR end-state | 🔧 open (relabel target-state vs live) |
| CX-X2 | HIGH | cross-skill | Mapster assumed in 2 skills but absent from the stack | 🔧 open (= CX-2; ⚠️ user decision) |

### F. Process problems (flow/coordination, not artifact defects)
| ID | Problem | Status |
|----|---------|--------|
| PR-1 | In-context critic — architect ran `nexus:critic` in its OWN context (subagent spawn unavailable from within) → author≠reviewer violation in spirit | ✅ mitigated (TL spawned fresh-context critic for the real gate) |
| PR-2 | Two-phase spawn COLLAPSE — developer analyzed AND implemented in one run; `developer:analyze` gate watches sprint-rituals tree but skills land in the plugin repo → CROSS-REPO GATE BLIND SPOT | ⚠️ lesson (no checkpoint skipped — 0 questions; gate fix proposed for the architect's lessons) |
| PR-3 | Codex relay truncation — wrapper returns "Done."/"Acknowledged."/"Ready." instead of the report | ✅ mitigated (Codex writes reports to delivery files; TL reads from disk) |
| PR-4 | Non-Pass-1 changes in the plugin repo: `docs/architecture/README.md` (ADR-10 spawn-mode, pre-existing — mtime 12:52 ≪ dev 13:37) + `.claude/`/`.omc/`/`docs/audit/` runtime pollution | ⚠️ excluded from Pass 1 commit; flagged to user |

